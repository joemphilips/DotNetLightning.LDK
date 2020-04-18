namespace DotNetLightning.Data.NRustLightningTypes

open System
open System.IO
open ResultUtils
open DotNetLightning.Utils.Primitives
open DotNetLightning.Core.Utils.Extensions
open DotNetLightning.Serialize
open DotNetLightning.Utils
open NBitcoin

type FundingGenerationReadyData = {
    TemporaryChannelId: ChannelId
    ChannelValueSatoshis: uint64
}

type FundingBroadcastSafeData = {
    OutPoint: LNOutPoint
    UserChannelId: ChannelId
}

type PaymentReceivedData = {
    PaymentHash: PaymentHash
    Amount: LNMoney
}
type PaymentFailedData = {
    PaymentHash: PaymentHash
    RejectedByDest: bool
}

type Duration = {
    Secs: uint64
    Nanos: uint32
}

type StaticOutputData = {
    Outpoint: LNOutPoint
    Output: TxOut
}
type DynamicOutputP2WSHData = {
    Outpoint: LNOutPoint
    Key: Key
    WitnessScript: Script
    ToSelfDelay: uint16
    Output: TxOut
}

type DynamicOutputP2WPKHData = {
    Outpoint: LNOutPoint
    Key: Key
    Output: TxOut
}

type SpendableOutputDescriptor =
    | StaticOutput of StaticOutputData
    | DynamicOutputP2WSH of DynamicOutputP2WSHData
    | DynamicOutputP2WPKH of DynamicOutputP2WPKHData
    with
    static member Parse(bytes: byte[]): Result<SpendableOutputDescriptor, _> =
        match bytes.[0] with
        | 0uy ->
            let outputs = TxOut()
            outputs.FromBytes(bytes.[37..])
            {
                StaticOutputData.Outpoint = LNOutPoint.FromBytes(bytes.[1..36])
                Output = outputs
            }
            |> StaticOutput
            |> Ok
        | 1uy ->
            result {
                let! witnessScriptLen, _ = bytes.[(1 + 36 + 31)..].TryPopVarInt()
                let witnessScriptLen = int witnessScriptLen
                let output = TxOut()
                output.FromBytes(bytes.[(1 + 36 + 32 + witnessScriptLen + 2)..])
                return {
                    // 36 bytes
                    DynamicOutputP2WSHData.Outpoint = LNOutPoint.FromBytes(bytes.[1..36])
                    // 32 bytes
                    Key = Key(bytes.[(1 + 36)..(1 + 36 + 32 - 1)])
                    WitnessScript = Script(bytes.[(1 + 36 + 32)..(1 + 36 + 32 + witnessScriptLen - 1)])
                    ToSelfDelay = UInt16.FromBytesBigEndian(bytes.[(1 + 36 + 32 + witnessScriptLen)..(1 + 36 + 32 + witnessScriptLen + 2 - 1)])
                    Output = output
                }
                |> DynamicOutputP2WSH
            }
        | 2uy ->
            let output = TxOut()
            output.FromBytes(bytes.[(1 + 36 + 32)..])
            {
                DynamicOutputP2WPKHData.Outpoint = LNOutPoint.FromBytes(bytes.[1..36])
                Key = Key(bytes.[(1 + 36)..(1 + 36 + 32 - 1)])
                Output = output
            }
            |> DynamicOutputP2WPKH
            |> Ok
        | x ->
            Error(sprintf "Unknown SpendableOutputDescriptor type %A" x)
        
    member this.BytesLength =
        match this with
        | StaticOutput x ->
            1 + 36 + x.Output.ToBytes().Length
        | DynamicOutputP2WSH x ->
            1 + 36 + 32 + x.WitnessScript.ToBytes().Length + 2 + x.Output.ToBytes().Length
        | DynamicOutputP2WPKH x ->
            1 + 36 + 32 + x.Output.ToBytes().Length
            
    member this.ToBytes() =
        use ms = new MemoryStream()
        use s = new LightningWriterStream(ms)
        match this with
        | StaticOutput d ->
            s.Write(0uy)
            s.Write(d.Outpoint.Value.ToBytes())
            s.Write(d.Output.ToBytes())
        | DynamicOutputP2WSH d ->
            s.Write(1uy)
            s.Write(d.Outpoint.Value.ToBytes())
            s.Write(d.Key.ToBytes())
            s.Write(d.WitnessScript.ToBytes())
            s.Write(d.ToSelfDelay.GetBytesBigEndian())
            s.Write(d.Output.ToBytes())
        | DynamicOutputP2WPKH d ->
            s.Write(2uy)
            s.Write(d.Outpoint.Value.ToBytes())
            s.Write(d.Key.ToBytes())
            s.Write(d.Output.ToBytes())
        ms.ToArray()
            
    static member ParseUnsafe(b: byte[]) =
        match SpendableOutputDescriptor.Parse b with
        | Ok r -> r
        | Error e -> raise <| FormatException(e)

type Event =
    | FundingGenerationReady of FundingGenerationReadyData
    | FundingBroadcastSafe of FundingBroadcastSafeData
    | PaymentReceived of PaymentReceivedData
    | PaymentSent of PaymentPreimage
    | PaymentFailed of PaymentFailedData
    | PendingHTLCsForwardable of Duration
    | SpendableOutputs of SpendableOutputDescriptor[]
    with
    
    static member Parse(s: byte[]): Result<Option<Event>, _> =
        match s.[0] with
        | 0uy ->
            Ok(None)
        | 1uy ->
            {
                FundingBroadcastSafeData.OutPoint = LNOutPoint.FromBytes(s.[1..36])
                UserChannelId = ChannelId(uint256(s.[(36 + 1)..], false))
            }
            |> FundingBroadcastSafe
            |> Some |> Ok
        | 2uy ->
            {
                PaymentReceivedData.PaymentHash = uint256(s.[1..32], false) |> PaymentHash
                Amount = LNMoney.FromBytes(s.[33..40])
            }
            |> PaymentReceived
            |> Some |> Ok
        | 3uy ->
            s.[1..32]
            |> PaymentPreimage.Create
            |> PaymentSent
            |> Some |> Ok
        | 4uy ->
            {
                PaymentFailedData.PaymentHash = uint256(s.[1..32], false) |> PaymentHash
                RejectedByDest = s.[33] = 1uy
            }
            |> PaymentFailed
            |> Some |> Ok
        | 5uy ->
            {
                Duration.Secs = 0UL
                Nanos = 0u
            }
            |> PendingHTLCsForwardable
            |> Some |> Ok
        | 6uy ->
            let len = UInt64.FromBytes(s.[1..8], false)
            let outputs = ResizeArray()
            let mutable result = Ok()
            let mutable pos = 0
            for _ in 0UL..(len - 1UL) do
                match SpendableOutputDescriptor.Parse s with
                | Ok s ->
                    outputs.Add(s)
                    pos <- pos + s.BytesLength
                | Error e -> result <- Error e
            result
            |> Result.map(fun _ ->
            outputs.ToArray()
            |> SpendableOutputs
            |> Some
            )
        | x ->
            Error(sprintf "Unknown event type %A" x)
        
    static member ParseUnsafe(s:byte[]): Option<Event> =
        match Event.Parse(s) with
        | Ok s -> s
        | Error e -> raise <| FormatException(e)
        
    static member ParseMany(s: byte[]): Result<Event[], _> =
        let len = int(UInt16.FromBytesBigEndian(s.[0..1]))
        let res = Array.zeroCreate(len)
        let mutable pos = 0
        let mutable error = Ok()
        let mutable breaked = false
        let i = 0
        while (not <| breaked) && i < len do
            let e = Event.Parse(s.[pos..])
            match e with
            | Error e -> error <- Error(e)
            | Ok(None) -> breaked <- true
            | Ok(Some e) ->
                pos <- e.BytesLength
                res.[i] <- e
        error |> Result.map(fun _ -> res)
        
    static member ParseManyUnsafe(s: byte[]): Event[] =
        match Event.ParseMany(s) with
        | Ok s -> s
        | Error e -> raise <| FormatException(e);
    member this.BytesLength =
        match this with
        | FundingGenerationReady _ ->
            1
        | FundingBroadcastSafe _ ->
            1 +
            (32 + 4) // outpoint
            + 32 //  channel id
        | PaymentReceived _ -> 1 + 40
        | PaymentSent _ -> 1 + 32
        | PaymentFailed _ -> 1 + 33
        | PendingHTLCsForwardable _ -> 1
        | SpendableOutputs outputs ->
            1 + (outputs |> Array.sumBy(fun o -> o.BytesLength))