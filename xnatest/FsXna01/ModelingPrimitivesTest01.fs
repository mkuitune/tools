module Editor

(*
#r @"C:\Program Files (x86)\MonoGame\v3.0\Assemblies\Windows\MonoGame.Framework.dll"
open System.Collections.Generic
module TT = 
    type T(i:int) = 
        let ii = i
        do
            printfn "Init t"
        member x.I = ii
                
    let tcompare = 
        { new IEqualityComparer<_> with
            member x.Equals(a:T,b:T) = 
                a = b
            member x.GetHashCode(s:T) =
                s.GetHashCode()}

let s3 = HashSet<TT.T>(TT.tcompare);;
*)

open System
open System.Collections.Generic
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Microsoft.Xna.Framework.Storage
open Microsoft.Xna.Framework.GamerServices
open System.Linq
open GeometryUtils


// TODO: 
// 0. Model of render pipeline, 2d and 3d layers , selection and state sets
// 0.1 Widget hover, click and drag actions
// 1. store and loads models from scene
// 1.1 CSG tree
// 1.2 Triangle meshes
// 1.3 Generator objects wih parametric tesselation
// 2. Add grid background as workspace walls
// 3. Arcball rotation
// 3.1 Scene navigation
// 3.2 Zoom in/out
// 4. Select objects
// 5. Add transform widget for selected objects or workplane + uparrow visualization
// 6. Add Grouping command for selected objects
// 7. Generator command for mesh data
// 8. Merge and cut widgets
// 9. Mesh shape generators (cube, sphere, etc)
// 10. Export to STL
// 11. CSG operations on models
// 12. Main thread polls input, worker thread renders. 

type View() = 
    let mutable azx = 0. // z = r cos axy, x = r sin axy 
    let mutable ay = Math.PI * 0.5  // y = 1.0 - r cos az
    let mutable r = 3.   
    
    let mutable up = v3(0,1,0) 
    let mutable target = v3(0, 0, 0)
    let mutable aspectRatio = 1.0f
    //let mutable pos = v3(0, 0, r)
  
    // TODO: Rotate around y, rotate in plane 
    member this.PosX = r  * (sin azx) * (sin ay)
    member this.PosY = r * (cos ay)
    member this.PosZ = r  * (cos azx) * (sin ay)
    member this.RotateOffplane doff =  ay <- ay + doff
    member this.RotateInplane dazx = azx <- (azx + dazx) % (Math.PI * 2.0)
    member this.Projection  =  
        Matrix.CreatePerspectiveFieldOfView(pi_4, aspectRatio, 0.1f, 100.0f)
    member this.ViewMatrix = 
        let pos = v3(this.PosX, this.PosY, this.PosZ)
        Matrix.CreateLookAt(pos, target, up)
    member this.SetAspectRatio (r:float32) = aspectRatio <- r

module Presentation = 
    type Primitive = |Triangle | Line

/// IRenderable
type IRenderable = 
    abstract member VertexBuffer : VertexBuffer with get
    abstract member PrimitiveCount: int with get
    abstract member World : Matrix with get
    abstract member Presentation : Presentation.Primitive

// Grid to visualize scene limits
type BackgroundGrid(g:GraphicsDevice) = 
    let gridColor = Color.DarkSlateGray
    let gv(x, y, z) = vpc(gridColor, v3(x, y, z))
    let vertexType = typeof<VertexPositionColor>
    let mutable vertexData = [|gv(0, 1, 0); gv(1, -1, 0); gv(1, -1, 0); gv(-1, -1, 0); gv(-1, -1, 0);gv(0, 1, 0)|] // todo gridlines
    let mutable vertexBuffer = new VertexBuffer(g, vertexType, 3, BufferUsage.None)
    let toWorld = Matrix.Identity //> world matrix

    do
        vertexBuffer.SetData<VertexPositionColor>(vertexData)

    member this.UpdateData() = vertexBuffer.SetData<VertexPositionColor>(vertexData)
    member this.BuildGrid() = 
        // Compute
        let dim = 1.0
        let max = v3(dim, dim, dim)
        let min = max * (-1.f)
        ()
//        let gridpoints = [||]

    member this.VertexBuffer  = vertexBuffer
    member this.PrimitiveCount=  vertexData.Length / 3
    member this.World  = toWorld
    member this.Presentation = Presentation.Line 
    interface IRenderable with
        member this.VertexBuffer with get() = this.VertexBuffer
        member this.PrimitiveCount with get() =  this.PrimitiveCount
        member this.World with get() = this.World
        member this.Presentation with get() = this.Presentation


//> Vertex source + vertex buffer ref
type RGBTriangle(g:GraphicsDevice) = 
    let mutable vertexData = [|vpc(Color.Red, v3(0, 1, 0)); vpc(Color.Green, v3(1, -1, 0)); vpc(Color.Blue, v3(-1, -1, 0))|]
    let vertexType = typeof<VertexPositionColor>
    let vertexBuffer = new VertexBuffer(g, vertexType, 3, BufferUsage.None)

    let toWorld = Matrix.Identity //> world matrix

    do
        vertexBuffer.SetData<VertexPositionColor>(vertexData)

    member this.VertexBuffer with get() = vertexBuffer
    member this.PrimitiveCount with get() =  vertexData.Length / 3
    member this.World with get() = toWorld
    member this.Presentation = Presentation.Triangle
    interface IRenderable with
        member this.VertexBuffer with get() = this.VertexBuffer
        member this.PrimitiveCount with get() =  this.PrimitiveCount
        member this.World with get() = this.World
        member this.Presentation with get() = this.Presentation

type ShadingContext(g:GraphicsDevice) = 
    let mutable effect = new BasicEffect(g, VertexColorEnabled = true)
    member this.Effect = effect
    member this.SetTransforms(r:IRenderable) (v:View) =  
        effect.World <-r.World
        effect.View <- v.ViewMatrix
        effect.Projection <- v.Projection

let drawWith(r:IRenderable, v:View, s:ShadingContext, g:GraphicsDevice) = 
    s.SetTransforms r v
    g.SetVertexBuffer(r.VertexBuffer)
    s.Effect.CurrentTechnique.Passes.[0].Apply()
    match r.Presentation with
    |Presentation.Line -> g.DrawPrimitives(PrimitiveType.LineList, 0, r.PrimitiveCount)
    |Presentation.Triangle -> g.DrawPrimitives(PrimitiveType.TriangleList, 0, r.PrimitiveCount)
//    g.DrawPrimitives(PrimitiveType.LineList, 0, r.PrimitiveCount)
    g.Flush()

// Store all the stuff that depedends on a valid graphics device here for 2d
//type Engine2D(g:GraphicsDevice) = 
    //let 
    // TODO: Draw 2D stuff here
type PointerPos(pos:Vector2, delta:Vector2) = 
    member m.Pos = pos
    member m.Delta = delta
    member m.Next v = PointerPos(v, v - pos)

module Switch = 
    type Key = Microsoft.Xna.Framework.Input.Keys
    type SwitchState = | Pressed | Released
    type SwitchId =
        | Char of Key (* Can be separated to own enum if needed.*)
        | Indexed of int (*E.g. with XNA mouse 0: left 1: right*)

    module Pointer =
        let Left = 0
        let Right = 1

    type Switch = {State:SwitchState; Id:SwitchId}
    
    type StateSet() = 
        let mutable switches = new Dictionary<SwitchId, Switch>()
        let mutable indexedDelta : Switch list = [] 
        let mutable charDelta : Switch list = []

        member x.BeginFrame() = 
            indexedDelta <- []
            charDelta <- []

        member x.Update (sw:Switch) =
            if not (switches.ContainsKey(sw.Id)) then
               switches.Add(sw.Id, sw) 
            else
                let curval = switches.[sw.Id]
                if curval.State <> sw.State then
                    match sw.Id with
                    | Indexed(_) -> indexedDelta <- sw :: indexedDelta
                    | Char(_) -> charDelta <- sw :: charDelta
                    switches.[sw.Id] <- sw

        member x.CharDelta = charDelta
        member x.IndexedDelta = indexedDelta

        member x.Update(swl : Switch list) = swl |> List.iter (fun e -> x.Update(e))
            
        member x.Pressed (k:SwitchId) = if switches.ContainsKey(k) then match switches.[k].State with |Pressed -> true | _ -> false else false
        member x.Pressed (ki:int) = x.Pressed(Indexed(ki))
        member x.Pressed (k:Key) = x.Pressed(Char(k))

let mouseToSwitches(ms:MouseState) = 
    let bs = function
    | Microsoft.Xna.Framework.Input.ButtonState.Pressed -> Switch.Pressed
    | _ -> Switch.Released
    let sw state id = {Switch.State = state; Switch.Id = Switch.Indexed(id)}
    [(sw (ms.LeftButton |> bs) Switch.Pointer.Left); (sw (ms.RightButton |> bs) Switch.Pointer.Right)]

let keyboardToSwitches(kb:KeyboardState) = 
    let sw state id = {Switch.State = state; Switch.Id = Switch.Char(id)}
    let allKeys = Enum.GetValues(typeof<Switch.Key>).Cast<Switch.Key>()
    [for k in allKeys do 
        match kb.IsKeyDown(k) with
        | true -> yield (sw Switch.Pressed k)
        | false -> yield(sw Switch.Released k)]
        

type PointerHandler() =
    let mutable pos = PointerPos(v2(0,0), v2(0,0))
    member m.Delta = pos.Delta
    member m.Next (ms:MouseState) =
        let v  = v2(ms.Position.X, ms.Position.Y)
        pos <- pos.Next(v)
        if pos.Delta <> v2(0,0) then
            printfn "%A %A" pos.Pos.X pos.Pos.Y

// Generic asset manager
type AssetManager(manager: ContentManager) = 
    let tex2d = Dictionary<string, Texture2D>()
    member this.LoadTexture2D(name:string) =
        if tex2d.ContainsKey(name) |> not then 
            tex2d.[name] <- manager.Load<Texture2D>(name)
        tex2d.[name]
 (*
type HoverState =
    |NonHovering
    |Hovering
    |Selected

type ButtonState =
    | ButtonDown
    | ButtonPressed // Sudden change
    | ButtonLifted
    | ButtonUp
*)

// TODO: Proper 2D scene graph for ui assets for parenting etc.
// 2D buttons sliders etc manages graphcis assets and geometric bounds
type SpriteWidget(textureName:string, bgrName:string,rect:Rectangle, manager:AssetManager) = 
    let name = textureName
    let bounds = rect 
    let mutable textures : Texture2D list = []
    //let mutable hoverState = NonHovering
    do
        textures <- [manager.LoadTexture2D(name)]
        // Background first, render in order of loading
        if bgrName <> "" then
            textures <- manager.LoadTexture2D(bgrName) :: textures
      
    member this.Contains(p:Vector2) =
        bounds.Contains(p)

    member this.DrawTo(batch:SpriteBatch) =
        for t in textures do 
            batch.Draw(t, bounds, Color.White)

type UI2D(manager:AssetManager, g:GraphicsDevice) = 
    let mutable manager = manager 
    let mutable spriteBatch : SpriteBatch = null
    let mutable buttons : SpriteWidget list = []
    do
        spriteBatch <- new SpriteBatch(g)

    member this.AddButton(textureName:string, bgr:string, rect:Rectangle) = 
        buttons <- SpriteWidget(textureName, bgr, rect, manager) :: buttons
    member this.Draw(time:GameTime, device:GraphicsDevice) =
        //spriteBatch.Begin()
        spriteBatch.Begin(SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.AnisotropicClamp,
            DepthStencilState.None,
            RasterizerState.CullNone)
        for b in buttons do
            b.DrawTo(spriteBatch)
        spriteBatch.End()
        ()
    (* Do mouse: check if mouse hovers over anything
        member this.DoMouse(pos:Vector2) = 
        false
      *)


// Store all the stuff that depedends on a valid graphics device here for 3d
type ScreenApplication(g:GraphicsDevice, cont:ContentManager) = 
    let mutable view = View()
    let mutable shadingContext = ShadingContext(g)
    let mutable pointer = new PointerHandler()
    let mutable assetManager = AssetManager(cont)
    let mutable ui2d = UI2D(assetManager, g)
    let mutable buttons = Switch.StateSet()
   
    // Content 
    let mutable renderable = RGBTriangle(g)
    let mutable grid = BackgroundGrid(g)

    member this.UI = ui2d
    member this.View = view

    member this.Draw(time:GameTime, device:GraphicsDevice) = 
        //device.Clear(Color.CornflowerBlue)
        device.Clear(Color.LightSlateGray)
        drawWith(renderable, view, shadingContext, g)
        //drawWith(grid, view, shadingContext, g)
        ui2d.Draw(time, device)

    member this.Update(time:GameTime, mouseState:MouseState, keyState : KeyboardState) = 
        // Update view parameters
        view.SetAspectRatio(g.Viewport.AspectRatio)
        // Handle mouse input
        pointer.Next(mouseState)        
        // Handle button input
        let buttonEvents = (mouseToSwitches mouseState) @ (keyboardToSwitches keyState)
        buttons.BeginFrame()
        buttons.Update(buttonEvents)
        if buttons.CharDelta <> [] then printfn "%A" buttons.CharDelta
        if buttons.IndexedDelta <> [] then printfn "%A" buttons.IndexedDelta
        // Handle events
        if buttons.Pressed(Switch.Pointer.Left ) then
            let factor = 0.1
            view.RotateInplane (-factor * (float pointer.Delta.X))
            view.RotateOffplane (factor * (float pointer.Delta.Y))

type ModelingTest02() as this = 
    inherit Game()

    // Private data
    let mutable d: ScreenApplication option = None 
    let mutable graphicsManager = new GraphicsDeviceManager(this)
    do
        base.Content.RootDirectory <- "Content"
        base.IsMouseVisible <- true

    member this.ConstructScene() = 
        ()
        //let loadall (app:ScreenApplication) =
        //    app.UI.AddButton("icontest-ball", "icontest-background", Rectangle(0,0, 100, 100))
        //match d with
        //| Some(app) -> loadall app
        //| _ -> ()

    // Overrides
    override this.Initialize() =
        base.Initialize()

    override this.LoadContent() = 
        d <-Some(ScreenApplication(this.GraphicsDevice, base.Content))
        this.ConstructScene()

    override this.UnloadContent() = ()

    override this.Update(time:GameTime) = 
        let kbstate = Keyboard.GetState()
        if kbstate.IsKeyDown(Keys.Escape) then
            base.Exit()

        match d with
        | Some(engine) ->engine.Update(time, Mouse.GetState(), kbstate)
        |_ ->()


        base.Update(time)
        ()

    override this.Draw(time:GameTime) = 
        match d with
        |Some(engine) -> engine.Draw(time, base.GraphicsDevice)
        | _ -> ()
        base.Draw(time)
        ()

(*

type BufferedGraphicsScene(g:GraphicsDevice) = 
    let v = VertexPositionColor()
    let mutable vertexData = [|vpc(Color.Red, v3(0, 1, 0)); vpc(Color.Green, v3(1, -1, 0)); vpc(Color.Blue, v3(-1, -1, 0))|]
    let view = Matrix.CreateLookAt(v3(0,0,3), v3(0,0,0), v3(0,1,0)) in
    let world = Matrix.Identity in
    let projection =  Matrix.CreatePerspectiveFieldOfView(pi_4, g.Viewport.AspectRatio, 0.1f, 100.0f) in
    let mutable effect = new BasicEffect(g, World = world, View = view, 
                                         Projection = projection, VertexColorEnabled = true)
    let vertexType = typeof<VertexPositionColor>
    let vertexBuffer = new VertexBuffer(g, vertexType, 3, BufferUsage.None)
    
    do
        vertexBuffer.SetData<VertexPositionColor>(vertexData)

    member this.Effect = effect
    member this.Apply() = effect.CurrentTechnique.Passes.[0].Apply()
    member this.Draw() = 
        g.SetVertexBuffer(vertexBuffer)
        effect.CurrentTechnique.Passes.[0].Apply()
        g.DrawPrimitives(PrimitiveType.TriangleList, 0, 1)

// Test model loading
type ModelingTest01() as this = 
    inherit Game()
    // View paraameters - lift out 
   
    let mutable lookat = Matrix.CreateLookAt(v3(2.,3.,-5), v3(0,0,0), Vector3.Up)
    let mutable persp = Matrix.CreatePerspectiveFieldOfView(pi_4, 1.0f, 0.1f, 1000.0f)

    // 
    let mutable graphics = new GraphicsDeviceManager(this)
    let mutable myEffect : BufferedGraphicsScene option = None

    let mutable model : Model option = None

    do
        base.Content.RootDirectory <- "Content"

    override this.Initialize() =
        base.Initialize()

    override this.LoadContent() = 
        lookat <- Matrix.CreateLookAt(v3(2.,3.,-5), v3(0,0,0), Vector3.Up)
        persp <- Matrix.CreatePerspectiveFieldOfView(pi_4, this.GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000.0f)
        myEffect <- Some(BufferedGraphicsScene(this.GraphicsDevice))
        model <- Some(base.Content.Load<Model>("box"))
        //model <- Some(base.Content.Load<Model>("spikeplane"))
        let eff = match model with 
                    | Some(m) -> m.Meshes.[0].Effects.[0] :?> BasicEffect
                    |_ -> null
        eff.EnableDefaultLighting()


    override this.UnloadContent() = ()

    override this.Update(time:GameTime) = 
        if Keyboard.GetState().IsKeyDown(Keys.Escape) then
            base.Exit()
        base.Update(time)
        ()
    override this.Draw(time:GameTime) = 
        base.GraphicsDevice.Clear(Color.CornflowerBlue)
        let scale = 0.01
        // Model
        let world = Matrix.CreateScale(v3(scale, scale, scale))
        match model with 
        | Some (m) ->   m.Draw(world, lookat, persp)
        |_ -> () 

        base.Draw(time)
        ()
        *)

