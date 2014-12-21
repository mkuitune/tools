// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

open System
open System.Collections.Generic
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Microsoft.Xna.Framework.Storage
open Microsoft.Xna.Framework.GamerServices

open xnabook1
open ModelingPrimitivesTest01

//
// Icosahedron game
//
module Icosahedron =
  let vertex =
    let vec3 x y z = x, y, z
    let r = sin(System.Math.PI / 3.)
    let s = 1. / sqrt 5.
    let f g x = r * g(float x * System.Math.PI / 5.)
    let aux n y i = vec3 (f sin (2 * i + n)) y (f cos (2 * i + n))
    [|yield vec3 0. 1. 0.
      for i in 0 .. 4 do yield aux 0 s i
      for i in 0 .. 4 do yield aux 1 (-s) i
      yield vec3 0. -1. 0.|]

  let index =
    let r1 i = 1 + (1 + i) % 5
    let r2 i = 6 + (1 + i) % 5
    [|for i in 0 .. 4 do yield 0, r1 i, r1(i + 1)
      for i in 0 .. 4 do yield r1(i + 1), r1 i, r2 i
      for i in 0 .. 4 do yield r1(i + 1), r2 i, r2(i + 1)
      for i in 0 .. 4 do yield r2(i+1), r2 i, 11|]


type Vertex = struct
    val Position : Vector3
    val Normal : Vector3
    val Color : Vector4
    new(p, n, c) = { Position = p; Normal = n; Color = c }
    static member VertexDeclaration() : VertexDeclaration =
        let o n = sizeof<float32> * n 
        new VertexDeclaration(
            [|  new VertexElement(o 0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0);
                new VertexElement(o 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0);
                new VertexElement(o 6, VertexElementFormat.Vector4, VertexElementUsage.Color, 0)|]
        ) 
 
  
//  static member VertexElements =
//    let ve3 = VertexElementFormat.Vector3
//    let vec = VertexElementFormat.Color
//    let m = VertexElementMethod.Default
//    let o n = sizeof<float32> * n |> int16
//    [|
//      VertexElement(0s, o 0, ve3, m, VertexElementUsage.Position, 0uy);
//      VertexElement(0s, o 3, ve3, m, VertexElementUsage.Normal, 0uy);
//      VertexElement(0s, o 6, vec, m, VertexElementUsage.Color, 0uy);
//    |]
end

let subdivide (ps: seq<Vector3 * Vector3 * Vector3>) =
  [|for p1, p2, p3 in ps do
      let f p1 p2 = Vector3.Normalize(p1 + p2)
      let p12, p23, p31 = f p1 p2, f p2 p3, f p3 p1
      for t in [p1, p12, p31; p12, p2, p23; p23, p3, p31; p31, p12, p23] do
        yield t|]

type XnaTest() as this =
  inherit Game()
  
  let mutable graphics : GraphicsDeviceManager = null
  let mutable effect : Effect = null
  let mutable vertexDeclaration : VertexDeclaration = null
  let mutable vertexBuffer : VertexBuffer = null
  let mutable projection : Matrix = Matrix.Identity
  let mutable view : Matrix = Matrix.Identity
  let mutable world : Matrix = Matrix.Identity

  let positions =
    [|for i, j, k in Icosahedron.index do
        let f n =
          let x, y, z = Icosahedron.vertex.[n]
          Vector3(float32 x, float32 y, float32 z)
        yield f i, f j, f k|]
    |> subdivide
    |> subdivide
  let vertex =
    [|for p1, p2, p3 in positions do
        for p in [p1; p2; p3] do
          yield Vertex(p, Vector3.Normalize p, Vector4(1.0f, 0.0f, 0.0f, 1.0f))|]
  let time = System.Diagnostics.Stopwatch.StartNew()
  
  do
    graphics <- new GraphicsDeviceManager(this)
    this.IsMouseVisible <- true

  override this.LoadContent() =
    effect <- this.Content.Load<Effect>("PhongTest") 
  
  override this.Initialize() =
    base.Initialize()
    let gd = graphics.GraphicsDevice
    
    vertexDeclaration <- Vertex.VertexDeclaration()
//    vertexBuffer <- new VertexBuffer(gd, vertexDeclaration, vertex.Length, BufferUsage.None)
    vertexBuffer <- new VertexBuffer(gd, vertexDeclaration, vertex.Length, BufferUsage.None)
    vertexBuffer.SetData(vertex)

//    let opts = CompilerOptions.None
//    let compiled =
//      let platform = TargetPlatform.Windows
//      Effect.CompileEffectFromSource(hlsl, [||], null, opts, platform)
//    printf "%s\n" compiled.ErrorsAndWarnings
//    effect <- new Effect(gd, compiled.GetEffectCode())
//    vertexDeclaration <-
//      let elts = Vertex.VertexElements
//      new VertexDeclaration(gd, elts)
  
  override this.Draw gameTime =
    let gd = graphics.GraphicsDevice
    gd.Clear Color.Black

    projection <-
      let fov = float32 System.Math.PI / 4.f
      let aspect =
        let bound = this.Window.ClientBounds
        float32 bound.Width / float32 bound.Height
      let near, far = 0.01f, 100.f
      Matrix.CreatePerspectiveFieldOfView(fov, aspect, near, far)
    
    let cameraPosition =
      let angle = float32 time.ElapsedMilliseconds / 1e3f
      Vector3(3.f * sin angle, 3.f, 3.f * cos angle)
    view <-
      let up = Vector3(0.f, 1.f, 0.f)
      Matrix.CreateLookAt(cameraPosition, Vector3.Zero, up)
    
    let draw (technique: string) (world: Matrix) =
        gd.SetVertexBuffer(vertexBuffer)
        //gd.RenderState.CullMode <- CullMode.None
        effect.CurrentTechnique <- effect.Techniques.[technique]
        effect.Parameters.["world"].SetValue(world)
        effect.Parameters.["view"].SetValue(view)
        effect.Parameters.["projection"].SetValue(projection)
        effect.Parameters.["lightPosition"].SetValue(Vector3(0.f, 2.f, 0.f))
        effect.Parameters.["cameraPosition"].SetValue cameraPosition
        effect.CurrentTechnique.Passes.[0].Apply() 

        gd.DrawPrimitives(PrimitiveType.TriangleList, 0, (vertex.Length/3))
        //gd.DrawPrimitives(PrimitiveType.TriangleList, 0, 0)
        
    //draw "Gouraud" (Matrix.CreateTranslation(-1.05f, 0.f, 0.f))
    draw "Phong" (Matrix.CreateTranslation(1.05f, 0.f, 0.f))
        
//      gd.VertexDeclaration <- vertexDeclaration
//      gd.RenderState.CullMode <- CullMode.None
//      effect.CurrentTechnique <- effect.Techniques.[technique]
//      effect.Parameters.["world"].SetValue(world)
//      effect.Parameters.["view"].SetValue(view)
//      effect.Parameters.["projection"].SetValue(projection)
//      effect.Parameters.["lightPosition"].SetValue(Vector3(0.f, 2.f, 0.f))
//      effect.Parameters.["cameraPosition"].SetValue cameraPosition
//      effect.Begin()
//      for pass in effect.CurrentTechnique.Passes do
//        pass.Begin()
//        let prim = PrimitiveType.TriangleList
//        gd.DrawUserPrimitives(prim, vertex, 0, vertex.Length / 3)
//        pass.End()
//      effect.End()
    



//
// Test game
//
type Game1() as this = 
    inherit Game()
    let mutable graphics = new GraphicsDeviceManager(this)
    let mutable spriteBatch : SpriteBatch = null

    do
        base.Content.RootDirectory <- "Content"

    override this.Initialize() = 
        base.Initialize()

    override this.LoadContent() =
        spriteBatch <- new SpriteBatch(base.GraphicsDevice)
        ()

    override this.UnloadContent() = ()

    override this.Update(gameTime : GameTime) = 
        if GamePad.GetState(PlayerIndex.One).Buttons.Back = ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape) then
            base.Exit()

        base.Update(gameTime)

        ()

    override this.Draw(gameTime:GameTime) = 
        base.GraphicsDevice.Clear(Color.CornflowerBlue)

        base.Draw(gameTime)
        ()



[<EntryPoint>]
let main argv = 
    printfn "%A" argv
//    let game = new Game1()
//    let game = new XnaTest()
//    let game = new XnaSample01()
//    let game = new XnaSample02()
//    let game = new XnaSample03()
//    let game = new XnaSample04()
//    let game = new ModelingTest01()
    let game = new ModelingTest02()
    game.Run()
    0 // return an integer exit code
