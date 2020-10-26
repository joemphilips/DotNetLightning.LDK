module NRustLightning.GUI.Client.Main2

open System
open Bolero
open Bolero.Html
open Bolero.Remoting
open Elmish
open MatBlazor
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.Web
open NRustLightning.GUI.Client.AppState
open Bolero.Templating.Client
open NRustLightning.GUI.Client.Configuration
open NRustLightning.GUI.Client.Utils
open NRustLightning.GUI.Client.Wallet
open NRustLightning.GUI.Client.Wallet.Utils

type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/wallet">] Wallet
    | [<EndPoint "/coinview">] CoinView
    | [<EndPoint "/config">] Configuration

type Model = {
    bbDrawerClass: string
    NavMenuOpened: bool
    NavMinified: bool
    Page: Page
    WalletNames: Deferred<Map<WalletId, string>>
}
    with
    static member Default = {
        bbDrawerClass = String.Empty
        NavMenuOpened = false
        NavMinified = true
        Page = Home
        WalletNames = HasNotStartedYet
    }
    
type Msg =
    | NavToggle
    | NavMinify
    | LoadWalletNames of AsyncOperationStatus<Map<WalletId, string>>
    | SetPage of Page

type Args = internal {
    AppState: AppState
}

type MainService = {
    GetWalletList: unit -> Async<Map<WalletId, string>>
}
    with
    interface IRemoteService with
        member this.BasePath = "/main"
    

let update service msg model =
    match msg with
    | NavToggle ->
        let opened =  not model.NavMenuOpened
        let bbDrawerClass = if opened then "full" else "closed"
        { model with NavMenuOpened = opened; bbDrawerClass = bbDrawerClass }, Cmd.none
    | NavMinify ->
        let minified = if (model.NavMenuOpened |> not) then true else not model.NavMinified
        let bbDrawerClass =
            if (minified) then "mini" else
            if (model.NavMenuOpened) then "full" else
            model.bbDrawerClass
        { model with bbDrawerClass = bbDrawerClass; NavMinified = minified }, Cmd.none
    | SetPage page ->
        { model with Page = page }, Cmd.none
    | LoadWalletNames(Started) ->
        let onSuccess = Finished >> LoadWalletNames
        let onError e = raise e
        { model with WalletNames = InProgress}, Cmd.OfAsync.either (service.GetWalletList) () onSuccess onError
    | LoadWalletNames(Finished names) ->
        { model with WalletNames = Resolved names}, Cmd.none
        
[<AutoOpen>]
module private ButtonWithTooltip =
            
    let private button icon onClick forwardRef =
        comp<MatIconButton> ["Class" => "navToggle"
                             "Icon" => icon
                             "ToggleIcon" => icon
                             "RefBack" => forwardRef
                             attr.callback "OnClick" (onClick)
                             ] []
    let buttonWithTooltip icon tooltip onClick =
        let buttonComp = button icon onClick
        let tooltip = comp<MatTooltip> ["Tooltip" => tooltip
                                        attr.fragmentWith "ChildContent" (fun (f: ForwardRef) -> buttonComp f)] []
        tooltip

let view ({ AppState = appState; })
         (model: Model)
         (dispatch) =
    comp<MatDrawerContainer> [
        "Style" => "width: 100vw; height: 100vh;"
        "Class" => model.bbDrawerClass
    ] [
        comp<MatDrawer> [
            "Opened" => model.NavMenuOpened
        ] [
            header [attr.classes["drawer-header"]] [
                div [attr.classes["drawer-logo"]] [
                    img [attr.alt appState.AppName; attr.classes["logo-img"]; attr.src "/images/bitcoin-svglogo.svg"; attr.title appState.AppName]
                    a [attr.classes ["miniHover"]; attr.href "/"] [text appState.AppName]
                ]
            ]
            comp<MatNavMenu> [ "Multi" => true; "Class" => "app-sidebar" ] [
                ecomp<NavMenu.EApp,_,_> [] NavMenu.Home (fun (_: NavMenu.Msg) -> Home |> SetPage |> dispatch)
                ecomp<NavMenu.EApp,_,_>
                    []
                    (model.WalletNames |> Deferred.toOption |> Option.map(Map.toList) |> Option.toList |> Seq.concat |> Map.ofSeq |> NavMenu.Wallet)
                    (fun _ -> Page.Wallet |> SetPage |> dispatch)
            ]
            footer [attr.classes ["drawer-footer"]] []
        ]
        comp<MatDrawerContent> [] [
            comp<MatAppBarContainer> ["Style" => "display: flex; flex-direction: column; min-height: 100vh;"] [
                comp<MatAppBar> ["Fixed" => true] [
                    comp<MatAppBarRow> [] [
                        comp<MatAppBarSection> [] [
                            comp<MatAppBarTitle> [] [
                                div [attr.classes ["hidden-mdc-down"]] [
                                    buttonWithTooltip "menu" "AppHoverNavToggle" (fun (_e: MouseEventArgs) -> dispatch NavToggle)
                                    buttonWithTooltip "format_indent_decrease" "AppHoverNavMinimize" (fun (_e: MouseEventArgs) -> dispatch NavMinify )
                                ]
                            ]
                        ]
                        comp<MatAppBarSection> ["Align" => MatAppBarSectionAlign.End] [
                            img [attr.alt appState.AppName; attr.classes["logo-img"]; attr.src "/images/bitcoin-svglogo.svg"; attr.title appState.AppName]
                        ]
                    ]
                ]
                comp<MatAppBarContent> ["Style" => "flex: 1; display: flex; flex-direction: column;"] [
                    comp<Breadcrumbs.App> [] []
                    section [ attr.classes["container-fluid"]; attr.style "flex: 1" ] [
                        cond model.Page <| function
                            | Home ->
                                text "this is child "
                            | Wallet ->
                                comp<WalletModule.App> [] []
                            | CoinView ->
                                comp<CoinViewModule.App> [] []
                            | Configuration ->
                                comp<ConfigurationModule.App> [] []
                    ]
                    footer [] []
                ]
            ]
        ]
    ]

type MyApp2() =
    inherit ProgramComponent<Model, Msg>()
    
    [<Inject>]
    member val AppState: AppState = Unchecked.defaultof<AppState> with get, set
    
    override this.Program =
        assert (this.AppState |> box |> isNull |> not)
        let args = { AppState = this.AppState; }
        let service = this.Remote<MainService>()
        Program.mkProgram(fun _ -> Model.Default, Cmd.ofMsg(LoadWalletNames(Started))) (update service) (view args)
    #if DEBUG
        |> Program.withHotReload
    #endif
