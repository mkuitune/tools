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
    let game = new Game1()
    game.Run()
    0 // return an integer exit code
