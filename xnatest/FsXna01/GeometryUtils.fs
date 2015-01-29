module GeometryUtils

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

let inline v3 (x: ^a, y: ^b, z:^c) = Vector3(float32 x, float32 y, float32 z)
let inline v2 (x: ^a, y: ^b) = Vector2(float32 x, float32 y)
let vpc(c, v:Vector3) = VertexPositionColor(Color = c, Position = v)
let pi_4 = MathHelper.PiOver4 
let myVertexArray = [|vpc(Color.Red, v3(0,1,0));vpc(Color.Red, v3(0, 1, 0))|]

let intToShort (ilist:int[]): int16 [] = [|for i in ilist -> int16 i|]

let inline rectangle(x:^a,  y :^a, w:^a, h:^a) = Rectangle(int x, int y, int w, int h)

