
open System.IO

let inputstring = """0.12967173315847241, -0.0064125000000000310, 0.38499999999999995
0.12967173315847241, -0.0064125000000000085, 0.31908749999999997
0.12967173315847241, 0.0035177579763121139, 0.31908749999999997
0.12967173315847241, 0.0035177579763121364, 0.38499999999999995
0.12967173315847241, 0.0058013720009735840, 0.39648050297095278
0.12967173315847241, 0.012304554540715693, 0.40621320343559641
0.12967173315847241, 0.022037255005359413, 0.41271638597533850
0.12967173315847241, 0.033517757976312103, 0.41500000000000009
0.12967173315847241, 0.14551775797631208, 0.41500000000000009
0.12967173315847241, 0.14551775797631208, 0.44500000000000006
0.12967173315847241, -0.14841250000000000, 0.44500000000000006
0.12967173315847241, -0.14841250000000000, 0.41500000000000009
0.12967173315847241, -0.036412500000000000, 0.41500000000000009
0.12967173315847241, -0.024931997029047307, 0.41271638597533850
0.12967173315847241, -0.015199296564403587, 0.40621320343559641
0.12967173315847241, -0.0086961140246614790, 0.39648050297095278"""


let inputstring2 = """0.12967173315847241, -0.0064125000000000310, 0.38499999999999995
0.12967173315847241, -0.0064125000000000085, 0.31908749999999997"""

/////////////////////////////////////////////////////////////////
// Utilities and shorthands
/////////////////////////////////////////////////////////////////

let inQuotes var = 
    "\"" + string(var) + "\""

/////////////////////////////////////////////////////////////////
// Types
/////////////////////////////////////////////////////////////////

type Axis = |X |Y |Z

type Point2D(x:float, y:float) =
    let x = x
    let y = y

    member this.X = x
    member this.Y = y

    static member (*) (p:Point2D, s) = Point2D(p.X * s, p.Y * s)
    static member (+) (p:Point2D, q:Point2D) = Point2D(p.X + q.X, p.Y + q.Y)
    static member (-) (p:Point2D, q:Point2D) = Point2D(p.X - q.X, p.Y - q.Y)

type Point = {x:float; y:float; z:float}
    with
        member this.project(a:Axis) : Point2D = 
            match a with
            |X -> Point2D(this.y, this.z)
            |Y -> Point2D(this.x, this.z)
            |Z -> Point2D(this.x, this.y)

        member this.at(a:Axis) = 
            match a with
            |X -> this.x
            |Y -> this.y
            |Z -> this.z

        /// Presumes elements in string array are castable to floats
        static member from(v:string []) =
            {x = float(v.[0]); y = float(v.[1]); z = float(v.[2])}

type ParamValue =
    |ParamString of string
    |ParamFloat of float
    |ParamInteger of int
    with 
    override this.ToString() = 
        match this with
        |ParamString str -> string(str)
        |ParamFloat flt -> string(flt)
        |ParamInteger ival -> string(ival)

type Param(name:string, value:ParamValue) =
    let name = name
    let value = value
    member this.DeclString() = name + "=" + (value.ToString() |> inQuotes) 
    static member Get(name, value:float):Param = Param(name, ParamFloat(value))
    static member Get(name, value:int):Param = Param(name, ParamInteger(value))
    static member Get(name, value:string):Param = Param(name, ParamString(value))

type DrawingArea(width:int, height:int) = 
    let width = width
    let height = height
    member this.Width = width
    member this.Height = width


/////////////////////////////////////////////////////////////////
// Point ops
/////////////////////////////////////////////////////////////////

let filterPoints (points : Point []) (skipAxis:Axis) =
    [| for p in points -> p.project(skipAxis)|]

let circleParams (x:float) (y:float) (radius:float) (stroke:string) (strokeWidth:float) (fill:string)=
    [|Param.Get("cx", x); Param.Get("cy", y); Param.Get("r", radius);
     Param.Get("stroke", stroke); Param.Get("stroke-width", strokeWidth); Param.Get("fill", fill)|]

let printElement elemName (paramList:Param []) =
    "<" + elemName + (Array.fold (fun out (elem:Param) -> out + " " + elem.DeclString()) "" paramList) + "/>"

// TODO scale points

let scalePointsToArea (area:DrawingArea) (padding:float) (points : Point2D []) =
    let getMin (out :Point2D) (p:Point2D) = 
        let x = if p.X < out.X then p.X else out.X
        let y = if p.Y < out.Y then p.Y else out.Y
        Point2D(x, y)

    let getMax (out :Point2D) (p:Point2D) = 
        let x = if p.X > out.X then p.X else out.X
        let y = if p.Y > out.Y then p.Y else out.Y
        Point2D(x, y)

    let minPoint = Array.fold getMin (Point2D(0., 0.)) points
    let maxPoint = Array.fold getMax (Point2D(0., 0.)) points

    let difPoint = maxPoint - minPoint
    let paddedWidth = float(area.Width) - 2. * padding
    let paddedHeight = float(area.Height) - 2. * padding
    let scaleX = paddedWidth / difPoint.X
    let scaleY = paddedHeight / difPoint.Y

    let scale = min scaleX scaleY

    let padPoint = Point2D(padding, padding)
    let mapPoints (p:Point2D) = (p - minPoint) * scale + padPoint

    Array.map mapPoints points

let pointsToSvgString (area:DrawingArea) (points : Point []) (skipAxis:Axis) =
    let padding = 10.0
    let w = area.Width
    let h = area.Height
    let widthParam = Param.Get("width", w)
    let heightParam = Param.Get("height", h)

    let radius = 3.0
    let stroke = "green"
    let strokeWidth = 1.0
    let fill = "yellow"

    let getCircleParams x y = circleParams x y radius stroke strokeWidth fill

    let emitAsCircles (p:Point2D) =
        (getCircleParams p.X p.Y) |> printElement "circle"

    let points2s = filterPoints points skipAxis |> scalePointsToArea area padding

    let pointsText = Array.fold (fun out p -> out + emitAsCircles p + "\n") "" points2s

    // Get as lines
    let mutable pointsLines = ""
    let quote p = (inQuotes (string(p)))
    let pCount = points2s.Length
    for i in 0 .. pCount - 1 do
        let first = points2s.[i]
        let second = points2s.[(i + 1) % pCount]
        let mid = (first + second) * 0.5
        // Draw line from first to second
        pointsLines  <- pointsLines + "<line x1=" + (quote first.X) +  " y1=" + (quote first.Y) +  " x2=" +  (quote second.X) + " y2=" +  (quote second.Y) + " style=" + (inQuotes "stroke:rgb(255,0,0);stroke-width:1")+ "/>" 
        pointsLines <- pointsLines + "<text x=" + (quote mid.X) + " y=" +  (quote mid.Y) + " fill=" + (inQuotes "black") + ">" + string(i) + "</text>\n"

    "<svg " + widthParam.DeclString()  + " " + heightParam.DeclString() + ">\n" + pointsText + "\n" + pointsLines + "\n</svg>"

let wrapInHtml elemIn = 
    "<html>\n<body>\n" + elemIn + "</body>\n</html>" 

//let pointsToSvgPath

let tokenizeString (str:string) : string [] = 
    str.Split([|' ';',';'\n'|], System.StringSplitOptions.RemoveEmptyEntries)

let stringToPoints (str:string) = 
    let toks = tokenizeString str
    [|for i in 0 .. 3 .. (toks.Length - 1) -> Point.from toks.[i .. i + 2]|]


let area = DrawingArea(600,600)

//tokenizeString inputstring2
let p = stringToPoints inputstring
let str = pointsToSvgString area p X
let html = wrapInHtml str
System.IO.File.WriteAllText("out.html", html)
