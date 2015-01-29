
module xnabook1

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

type XnaSample01() as this = 
    inherit Game()
    
    let mutable graphics = new GraphicsDeviceManager(this)
    let mutable spriteBatch  : SpriteBatch = null
    let mutable texture2D : Texture2D = null
    let mutable sfont : SpriteFont = null

    do
        base.Content.RootDirectory <- "Content"

    override this.Initialize() =
        base.Initialize()
        let adapter = GraphicsAdapter.Adapters
        for a in adapter do
            printfn "Adapter: %A" a

    override this.LoadContent() = 
        spriteBatch <- new SpriteBatch(this.GraphicsDevice)
        texture2D <- base.Content.Load<Texture2D>("ship1")
        sfont <- base.Content.Load<SpriteFont>("SpriteFont1")

    override this.UnloadContent() = ()

    override this.Update(time:GameTime) = 
        if Keyboard.GetState().IsKeyDown(Keys.Escape) then
            base.Exit()
        base.Update(time)
        ()
    override this.Draw(time:GameTime) = 
        base.GraphicsDevice.Clear(Color.CornflowerBlue)
        spriteBatch.Begin()
        //spriteBatch.Draw(texture2D, base.GraphicsDevice.Viewport.Bounds, Color.White)
        //spriteBatch.Draw(texture2D, Vector2.Zero, Color.White)
        let rect = rectangle(10, 10, 100, 100)
        spriteBatch.Draw(texture2D, rect, Color.White)
        spriteBatch.DrawString(sfont, "Foobaric tidings!", Vector2.Zero, Color.Black)
        spriteBatch.End()

        base.Draw(time)
        ()
    
//
// 3D graphics
//

let inline v3 (x: ^a, y: ^b, z:^c) = Vector3(float32 x, float32 y, float32 z)
let vpc(c, v:Vector3) = VertexPositionColor(Color = c, Position = v)
let pi_4 = MathHelper.PiOver4 
let myVertexArray = [|vpc(Color.Red, v3(0,1,0));vpc(Color.Red, v3(0, 1, 0))|]

let intToShort (ilist:int[]): int16 [] = [|for i in ilist -> int16 i|]

type VertexAndView(g:GraphicsDevice) = 
    let v = VertexPositionColor()
    let userTriangle = [|vpc(Color.Red, v3(0, 1, 0)); vpc(Color.Green, v3(1, -1, 0)); vpc(Color.Blue, v3(-1, -1, 0))|]
    let userLines = [|vpc(Color.White, v3(-1, 1, 0));
                      vpc(Color.White, v3(-1, -1, 0));
                      vpc(Color.White, v3(1, 1, 0));
                      vpc(Color.White, v3(1, -1, 0))|]
    let userQuadIndices =  intToShort [|0;1;2;1;3;2|]
    let userQuadVertices = [|vpc(Color.Red, v3(-1, 1, 0)); vpc(Color.Green, v3(1, 1, 0)); vpc(Color.Blue, v3(-1, -1, 0));vpc(Color.Purple, v3(1, -1, 0))|] 
    let view = Matrix.CreateLookAt(v3(0,0,3), v3(0,0,0), v3(0,1,0)) in
    let world = Matrix.Identity in
    let projection =  Matrix.CreatePerspectiveFieldOfView(pi_4, g.Viewport.AspectRatio, 0.1f, 100.0f) in
    let mutable effect = new BasicEffect(g, World = world, View = view, 
                                         Projection = projection, VertexColorEnabled = true)
    member this.Effect = effect
    member this.Apply() = effect.CurrentTechnique.Passes.[0].Apply()
    member this.Draw() = 
        g.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, userTriangle, 0, 1)
        g.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineStrip, userLines, 0, 3)
        g.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, userQuadVertices, 0, 4, userQuadIndices, 0, 2)


type XnaSample02() as this = 
    inherit Game()
    
    let mutable graphics = new GraphicsDeviceManager(this)
    let mutable myEffect : VertexAndView option = None

    do
        base.Content.RootDirectory <- "Content"

    override this.Initialize() =
        base.Initialize()

    override this.LoadContent() = 
        myEffect <- Some(VertexAndView(this.GraphicsDevice))

    override this.UnloadContent() = ()

    override this.Update(time:GameTime) = 
        if Keyboard.GetState().IsKeyDown(Keys.Escape) then
            base.Exit()
        base.Update(time)
        ()
    override this.Draw(time:GameTime) = 
        base.GraphicsDevice.Clear(Color.CornflowerBlue)
        match myEffect with
            |Some(e) -> e.Apply()
                        e.Draw()
            |_ -> ()

        base.Draw(time)
        ()

type BufferedGraphicsScene(g:GraphicsDevice) = 
    let v = VertexPositionColor()
    let vertexData = [|vpc(Color.Red, v3(0, 1, 0)); vpc(Color.Green, v3(1, -1, 0)); vpc(Color.Blue, v3(-1, -1, 0))|]
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

type XnaSample03() as this = 
    inherit Game()
    
    let mutable graphics = new GraphicsDeviceManager(this)
    let mutable myEffect : BufferedGraphicsScene option = None

    do
        base.Content.RootDirectory <- "Content"

    override this.Initialize() =
        base.Initialize()

    override this.LoadContent() = 
        myEffect <- Some(BufferedGraphicsScene(this.GraphicsDevice))

    override this.UnloadContent() = ()

    override this.Update(time:GameTime) = 
        if Keyboard.GetState().IsKeyDown(Keys.Escape) then
            base.Exit()
        base.Update(time)
        ()
    override this.Draw(time:GameTime) = 
        base.GraphicsDevice.Clear(Color.CornflowerBlue)
        match myEffect with
            |Some(e) -> e.Draw()
            |_ -> ()

        base.Draw(time)
        ()

// Test model loading
type XnaSample04() as this = 
    inherit Game()
    
    let mutable graphics = new GraphicsDeviceManager(this)
    let mutable myEffect : BufferedGraphicsScene option = None

    let mutable model : Model option = None

    do
        base.Content.RootDirectory <- "Content"

    override this.Initialize() =
        base.Initialize()

    override this.LoadContent() = 
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
        let t : float = (float time.TotalGameTime.TotalMilliseconds) / 1000.0
        let x = 10. * sin(t)
        let y = 10. * cos(t)
        let msg = sprintf "%A %A %A" t x y
        base.GraphicsDevice.Clear(Color.CornflowerBlue)
        let scale = 0.01
        let world = Matrix.CreateScale(v3(scale, scale, scale))
        let f = 7.0
//        let lookat = Matrix.CreateLookAt(v3(2. * f,3. * f,-5. * f), v3(x,y,0), Vector3.Up)
        let lookat = Matrix.CreateLookAt(v3(2.,3.,-5), v3(0,0,0), Vector3.Up)
        let persp = Matrix.CreatePerspectiveFieldOfView(pi_4, this.GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000.0f)
        match model with 
        | Some (m) ->   m.Draw(world, lookat, persp)
        |_ -> () 

        base.Draw(time)
        ()
