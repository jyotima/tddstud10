﻿module R4nd0mApps.TddStud10.Engine.Core.RunStepFuncBehaviorsTests

open Xunit
open System
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.Engine.TestFramework

let ex = new InvalidOperationException("A mock method threw")

let makeSpies (sb, erb, eb) = 
    new CallSpy<RunStepStartingEventArg>(sb), new CallSpy<RunStepErrorEventArg>(erb), new CallSpy<RunStepEndedEventArg>(eb)

let makeAndWireUpRSESpies2 (ss : CallSpy<RunStepStartingEventArg>, se : CallSpy<RunStepErrorEventArg>, sf : CallSpy<RunStepEndedEventArg>) = 
    let rses = 
        { onStart = new Event<_>()
          onError = new Event<_>()
          onFinish = new Event<_>() }
    (rses.onStart.Publish).Add(ss.Func >> ignore)
    (rses.onError.Publish).Add(se.Func >> ignore)
    (rses.onFinish.Publish).Add(sf.Func >> ignore)
    rses, (ss, se, sf)

let makeAndWireUpRSESpies () = 
    let (ss, se, sf) = makeSpies (DoesNotThrow, DoesNotThrow, DoesNotThrow)
    makeAndWireUpRSESpies2 (ss, se, sf)

let isHandlerCalled (s : CallSpy<RunStepStartingEventArg>) slnName stepName kind =
    s.CalledWith |> Option.map (fun r -> r.runData.startParams.solutionPath) = Some(~~slnName) 
    && s.CalledWith |> Option.map (fun r -> r.name) = Some(RunStepName stepName) 
    && s.CalledWith |> Option.map (fun r -> r.kind) = Some(kind) 

let isEndHandlerCalled (se : CallSpy<_>) slnName _ = 
    se.CalledWith |> Option.map (fun rss -> rss.runData.startParams.solutionPath) = Some(~~slnName) 

let isEndHandlerCalled2 (se : CallSpy<_>) slnName stepName status = 
    isEndHandlerCalled (se : CallSpy<_>) slnName stepName  
    && se.CalledWith |> Option.map (fun rss -> rss.status) = Some(status) 

let isEndHandlerCalled3 (se : CallSpy<_>) slnName stepName status addendum = 
    isEndHandlerCalled2 (se : CallSpy<_>) slnName stepName status
    && se.CalledWith |> Option.map (fun rss -> rss.addendum) = Some(addendum) 

[<Fact>]
let ``Events Publisher Behavior - Raises start, finish events if no failure``() = 
    let rd = RunExecutor.makeRunData DateTime.Now ~~"c:\\a\\b.sln"
    let rses, (ss, se, sf) = makeAndWireUpRSESpies()
    let f _ n k _ rd = 
        { name = n; kind = k; status = Succeeded; addendum = FreeFormatData("Some data"); runData = rd }
    (f |> RunStepFuncBehaviors.eventsPublisher) 1 (RunStepName "step name") Build rses rd |> ignore
    Assert.True(isHandlerCalled ss "c:\\a\\b.sln" "step name" Build)
    Assert.True(isEndHandlerCalled sf "c:\\a\\b.sln" "step name")
    Assert.False(isEndHandlerCalled se "c:\\a\\b.sln" "step name")

[<Fact>]
let ``Events Publisher - Handled errors - Raises start, error, finish events``() = 
    let rd = RunExecutor.makeRunData DateTime.Now ~~"c:\\a\\b.sln"
    let rses, (ss, se, sf) = makeAndWireUpRSESpies()
    let f _ n k _ rd = 
        { name = n; kind = k; status = Failed; addendum = FreeFormatData "Error Details"; runData = rd }
    let f1 = fun () -> ((f |> RunStepFuncBehaviors.eventsPublisher) 1 (RunStepName "step name") Test rses rd |> ignore)
    Assert.Equal(Assert.Throws<RunStepFailedException>(f1).Data0.addendum, FreeFormatData "Error Details")
    Assert.True(isHandlerCalled ss "c:\\a\\b.sln" "step name" Test)
    Assert.True(isEndHandlerCalled3 se "c:\\a\\b.sln" "step name" Failed (FreeFormatData("Error Details")))
    Assert.True(isEndHandlerCalled3 sf "c:\\a\\b.sln" "step name" Failed (FreeFormatData("Error Details")))

[<Fact>]
let ``Events Publisher - Unhandled errors - Raises start, error, finish events``() = 
    let rd = RunExecutor.makeRunData DateTime.Now ~~"c:\\a\\b.sln"
    let rses, (ss, se, sf) = makeAndWireUpRSESpies()
    let f _ n k _ rd = 
        raise (new TimeZoneNotFoundException())
        { name = n; kind = k; status = Succeeded; addendum = FreeFormatData("Some data"); runData = rd }
    let f1 = fun () -> ((f |> RunStepFuncBehaviors.eventsPublisher) 1 (RunStepName "step name") Test rses rd |> ignore)
    Assert.Throws<RunStepFailedException>(f1) |> ignore
    Assert.True(isHandlerCalled ss "c:\\a\\b.sln" "step name" Test)
    Assert.True(isEndHandlerCalled2 se "c:\\a\\b.sln" "step name" Aborted)
    Assert.True(isEndHandlerCalled2 sf "c:\\a\\b.sln" "step name" Aborted)

[<Fact>]
let ``Events Publisher - Handled errors - Raises all events even when all of them crash``() = 
    let (ss, se, sf) = makeSpies(Throws(ex), DoesNotThrow, DoesNotThrow)
    let rd = RunExecutor.makeRunData DateTime.Now ~~"c:\\a\\b.sln"
    let rses, _ = makeAndWireUpRSESpies2 (ss, se, sf)
    let f _ n k _ rd = 
        { name = n; kind = k; status = Failed; addendum = FreeFormatData "Error Details"; runData = rd }
    let f1 = fun () -> ((f |> RunStepFuncBehaviors.eventsPublisher) 1 (RunStepName "step name") Build rses rd |> ignore)
    Assert.Equal(Assert.Throws<RunStepFailedException>(f1).Data0.addendum, FreeFormatData "Error Details")
    Assert.True(ss.Called)
    Assert.True(isEndHandlerCalled3 se "c:\\a\\b.sln" "step name" Failed (FreeFormatData("Error Details")))
    Assert.True(isEndHandlerCalled sf "c:\\a\\b.sln" "step name")

[<Fact>]
let ``Events Publisher - Unhandled errors - Raises all events even when all of them crash``() = 
    let (ss, se, sf) = makeSpies(Throws(ex), Throws(ex), Throws(ex))
    let rd = RunExecutor.makeRunData DateTime.Now ~~"c:\\a\\b.sln"
    let rses, (ss, se, sf) = makeAndWireUpRSESpies2 (ss, se, sf)
    let f _ n k _ rd = 
        raise (new TimeZoneNotFoundException())
        { name = n; kind = k; status = Failed; addendum = FreeFormatData "Error Details"; runData = rd }
    let f1 = fun () -> ((f |> RunStepFuncBehaviors.eventsPublisher) 1 (RunStepName "step name") Build rses rd |> ignore)
    Assert.Throws<RunStepFailedException>(f1) |> ignore
    Assert.True(ss.Called)
    Assert.True(se.Called)
    Assert.True(sf.Called)

[<Fact>]
let ``Timer - Does not fail for funcs that dont fail``() = 
    let f _ _ _ _ rd = rd
    (f |> RunStepFuncBehaviors.stepTimer) 1 2 3 4 |> ignore
    Assert.True(true)

[<Fact>]
let ``Timer - Lets exceptions pass through``() = 
    let ex = new TimeZoneNotFoundException()
    
    let f _ _ _ _ rd = 
        raise ex
        rd
    
    let f1 = fun () -> ((f |> RunStepFuncBehaviors.stepTimer) 1 2 3 4 5 |> ignore)
    Assert.Equal(ex, Assert.Throws<TimeZoneNotFoundException>(f1))

[<Fact>]
let ``Logger - Does not fail for funcs that dont fail``() = 
    let f _ _ _ _ rd = rd
    (f |> RunStepFuncBehaviors.stepLogger) 1 2 3 4 |> ignore
    Assert.True(true)

[<Fact>]
let ``Logger - Lets exceptions pass through``() = 
    let ex = new TimeZoneNotFoundException()
    
    let f _ _ _ _ rd = 
        raise ex
        rd
    
    let f1 = fun () -> ((f |> RunStepFuncBehaviors.stepLogger) 1 2 3 4 5 |> ignore)
    Assert.Equal(ex, Assert.Throws<TimeZoneNotFoundException>(f1))
