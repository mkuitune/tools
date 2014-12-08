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
