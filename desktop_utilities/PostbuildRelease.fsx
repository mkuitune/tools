#load "DesktopUtilities.fs"

open DUP

// Include C:\bin in your path.
DUP.File.Copy(@"bin\Release\DesktopUtilities.dll", @"C:\bin\DesktopUtilities.dll")
