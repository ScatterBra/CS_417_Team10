using System;
class Test {
 static int n;
 static void Check(bool value){if(!value)throw new Exception("Failed "+(n+1));n++;}
 static void Main(){
 var normal=new DEF_ProgressionState(); normal.AcceptPrerequisite(DEF_ProgressionState.Prerequisite.Power6,1,"p"); Check(!normal.TeamReady);
 var s=new DEF_ProgressionState();s.ConfigureRecordingPowerOnly(); Check(!s.TeamReady);
 Check(!s.AcceptPrerequisite(DEF_ProgressionState.Prerequisite.Clinic10,1,"c"));
 Check(!s.AcceptPrerequisite(DEF_ProgressionState.Prerequisite.Power6,0,"p"));
 Check(!s.AcceptPrerequisite(DEF_ProgressionState.Prerequisite.Power6,1,""));
 Check(s.AcceptPrerequisite(DEF_ProgressionState.Prerequisite.Power6,1,"real")); Check(s.Current==DEF_ProgressionState.Stage.PlaceSentry);
 Check(!s.BeginWave(1,1)); Check(s.Place(1,1,"sentry")); Check(!s.Repair(1,1,"sentry",20));
 Check(s.CompleteIntroduction(1,1,s.Introduction));Check(s.Repair(1,1,"sentry",20));
 s.RetryDefense();Check(s.TeamReady);Check(s.Current==DEF_ProgressionState.Stage.PlaceSentry);
 s.NewSession();Check(!s.TeamReady);Check(s.RecordingPowerOnly);Console.WriteLine(n+" PASS");
 }
}
