module ModelingPrimitivesTest01

(*
#r @"C:\Program Files (x86)\MonoGame\v3.0\Assemblies\Windows\MonoGame.Framework.dll"
*)

open System
open System.Collections.Generic
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Microsoft.Xna.Framework.Storage
open Microsoft.Xna.Framework.GamerServices

open GeometryUtils



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


// TODO: 
// 1. store and loads models from scene
// 2. Add grid background as workspace walls
// 3. Arcball rotation
// 4. Select objects
// 5. Add transform widget for selected objects or workplane + uparrow visualization
// 6. Add Grouping command for selected objects
// 7. Generator command for mesh data
// 8. Merge and cut widgets
// 9. Mesh shape generators (cube, sphere, etc)
// 10. Export to STL
// 11. CSG operations on models

type View() = 
    let mutable azx = 0. // z = r cos axy, x = r sin axy 
    let mutable ay = 0.  // y = r cos az
    let mutable r = 3.   
    
    let mutable up = v3(0,1,0) 
    let mutable target = v3(0, 0, 0)
    let mutable pos = v3(0, 0, r)
  
    // TODO: Rotate around z, rotate in plane 
    // member this.RotateOffplane = 
    // member this.RotateInplane = 
    member this.Projection(g:GraphicsDevice)  =  
        Matrix.CreatePerspectiveFieldOfView(pi_4, g.Viewport.AspectRatio, 0.1f, 100.0f)
    member this.ViewMatrix = 
        Matrix.CreateLookAt(pos, target, up)

//> Vertex source + vertex buffer ref
type Renderable(g:GraphicsDevice) = 
    let mutable vertexData = [|vpc(Color.Red, v3(0, 1, 0)); vpc(Color.Green, v3(1, -1, 0)); vpc(Color.Blue, v3(-1, -1, 0))|]
    let vertexType = typeof<VertexPositionColor>
    let vertexBuffer = new VertexBuffer(g, vertexType, 3, BufferUsage.None)

    let toWorld = Matrix.Identity //> world matrix

    do
        vertexBuffer.SetData<VertexPositionColor>(vertexData)
    member this.VertexBuffer = vertexBuffer
    member this.PrimitiveCount =  vertexData.Length / 3
    member this.World = toWorld

type Material(g:GraphicsDevice) = 
    let mutable effect = new BasicEffect(g, VertexColorEnabled = true)
    member this.Effect = effect

type SceneVi3w(g:GraphicsDevice) = 
    let view = Matrix.CreateLookAt(v3(0,0,3), v3(0,0,0), v3(0,1,0)) in
    let world = Matrix.Identity in
    let projection =  Matrix.CreatePerspectiveFieldOfView(pi_4, g.Viewport.AspectRatio, 0.1f, 100.0f) in
    let mutable effect = new BasicEffect(g, World = world, View = view, 
                                         Projection = projection, VertexColorEnabled = true)
    member this.Draw(vb:VertexBuffer, count) = 
        g.SetVertexBuffer(vb)
        effect.CurrentTechnique.Passes.[0].Apply()
        g.DrawPrimitives(PrimitiveType.TriangleList, 0, count)


let drawWith(r:Renderable, v:View, m:Material, g:GraphicsDevice) = 
    m.Effect.World <- r.World
    m.Effect.View <- v.ViewMatrix
    m.Effect.Projection <- v.Projection(g)
    g.SetVertexBuffer(r.VertexBuffer)
    m.Effect.CurrentTechnique.Passes.[0].Apply()
    g.DrawPrimitives(PrimitiveType.TriangleList, 0, r.PrimitiveCount)


let drawIn (r:Renderable) (s:SceneVi3w) = 
   s.Draw(r.VertexBuffer, r.PrimitiveCount) 


// Store all the stuff that depedends on a valid graphics device here
type DwarfEngine(g:GraphicsDevice) = 
    let mutable view = View()
    let mutable renderable = Renderable(g)
    let mutable sceneview = SceneVi3w(g) 
    let mutable material = Material(g)

    member this.View = view
    member this.Draw() = 
       drawIn renderable sceneview
    member this.Draw2() = 
        drawWith(renderable, view, material, g)

type PointerPos(pos:Vector2, delta:Vector2) = 
    member m.Pos = pos
    member m.Delta = delta
    member m.Next v = PointerPos(v, pos - v)

type PointerHandler() =
    let mutable pos = PointerPos(v2(0,0), v2(0,0))

    member m.Next (ms:MouseState) =
        let v  = v2(ms.Position.X, ms.Position.Y)
        pos <- pos.Next(v)
        if pos.Delta <> v2(0,0) then
            printfn "%A %A" pos.Pos.X pos.Pos.Y
type ModelingTest02() as this = 
    inherit Game()

    // Private data
    let mutable pointer = new PointerHandler()
    let mutable d: DwarfEngine option = None 
    let mutable graphicsManager = new GraphicsDeviceManager(this)
    do
        base.Content.RootDirectory <- "Content"

    // Overrides
    override this.Initialize() =
        base.Initialize()

    override this.LoadContent() = 
        d <-Some(DwarfEngine(this.GraphicsDevice))

    override this.UnloadContent() = ()

    override this.Update(time:GameTime) = 
        if Keyboard.GetState().IsKeyDown(Keys.Escape) then
            base.Exit()

        // Handle mouse input
        let current = Mouse.GetState()
        pointer.Next(current)        

        base.Update(time)
        ()
    override this.Draw(time:GameTime) = 
        base.GraphicsDevice.Clear(Color.CornflowerBlue)
        match d with
        |Some(engine) -> engine.Draw2()
        | _ -> ()
        base.Draw(time)
        ()

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
