﻿namespace R4nd0mApps.TddStud10.Engine.Core

open System
open R4nd0mApps.TddStud10.Common.Domain

(* NOTE: 
   This should just be a wire up class. No business logic at all. 
   Hence unit tests not required. *)
type public TddStud10Runner private (re, agent, rst) = 
    
    static member public CreateRunStep(kind : RunStepKind, name : RunStepName, 
                                       func : Func<IRunExecutorHost, RunStepName, RunStepKind, RunData, RunStepResult>) : RunStep = 
        { kind = kind
          name = name
          func = fun h n k _ rd -> func.Invoke(h, n, k, rd) }
    
    static member public Create host runSteps = 
        let all f = 
            f
            |> RunStepFuncBehaviors.eventsPublisher
            |> RunStepFuncBehaviors.stepLogger
            |> RunStepFuncBehaviors.stepTimer
        
        let re = RunExecutor.Create host runSteps all
        let agent = new StaleMessageIgnoringAgent<DateTime * FilePath>(re.Start >> ignore)
        let rst = new RunStateTracker()
        re.RunStarting.Add(rst.OnRunStarting)
        re.RunStepStarting.Add(rst.OnRunStepStarting)
        re.OnRunStepError.Add(rst.OnRunStepError)
        re.RunStepEnded.Add(rst.OnRunStepEnd)
        re.OnRunError.Add(rst.OnRunError)
        re.RunEnded.Add(rst.OnRunEnd)
        new TddStud10Runner(re, agent, rst)
    
    member public __.AttachHandlers (rsc : Handler<RunState>) (sh : Handler<RunData>) (ssh : Handler<RunStepStartingEventArg>) 
           (serh : Handler<RunStepErrorEventArg>) (seh : Handler<RunStepEndedEventArg>) (erh : Handler<Exception>) 
           (eh : Handler<RunData>) = 
        rst.RunStateChanged.AddHandler(rsc)
        re.RunStarting.AddHandler(sh)
        re.RunStepStarting.AddHandler(ssh)
        re.OnRunStepError.AddHandler(serh)
        re.RunStepEnded.AddHandler(seh)
        re.OnRunError.AddHandler(erh)
        re.RunEnded.AddHandler(eh)

    member public __.DetachHandlers (eh : Handler<RunData>) (erh : Handler<Exception>) (seh : Handler<RunStepEndedEventArg>) 
           (serh : Handler<RunStepErrorEventArg>) (ssh : Handler<RunStepStartingEventArg>) (sh : Handler<RunData>) 
           (rsc : Handler<RunState>) = 
        re.RunEnded.RemoveHandler(eh)
        re.OnRunError.RemoveHandler(erh)
        re.RunStepEnded.RemoveHandler(seh)
        re.OnRunStepError.RemoveHandler(serh)
        re.RunStepStarting.RemoveHandler(ssh)
        re.RunStarting.RemoveHandler(sh)
        rst.RunStateChanged.RemoveHandler(rsc)

    member public __.StartAsync startTime slnPath token = agent.SendMessageAsync (startTime, FilePath slnPath) token
    member public __.StopAsync(token) = agent.StopAsync(token)
