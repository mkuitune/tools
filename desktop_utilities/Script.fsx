
#load "DesktopUtilities.fs"

open DUP

type Dir = DUP.Directory

let srcdir = @"D:\tmp\from"
let targetdir = @"D:\tmp\to"
Dir.Copy(srcdir, targetdir)
Dir.DeleteContents(targetdir)

