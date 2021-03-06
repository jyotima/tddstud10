﻿namespace R4nd0mApps.TddStud10.Hosts.VS.TddStudioPackage.Core.Editor

open System.Windows.Controls
open System.Windows

// NOTE: This class should not contain any business other than the l/t/w/h set + seq -> UIElementCollction copy.
// Hence this class is not covered by unit tests  
type MarginCanvas(getZL) as self = 
    inherit Canvas()
    
    do 
        self.ClipToBounds <- true
    
    member public self.Refresh(newChildren : (Rect * FrameworkElement) seq) = 
        let addChild (acc : UIElementCollection) ((r, e) : Rect * FrameworkElement) = 
            e.Height <- r.Height
            e.Width <- r.Width
            e.SetValue(Canvas.TopProperty, r.Top)
            e.SetValue(Canvas.LeftProperty, r.Left)
            acc.Add(e) |> ignore
            acc
        self.Children.Clear()
        self.Width <- MarginConstants.Width * getZL()
        newChildren
        |> Seq.fold addChild self.Children
        |> ignore
