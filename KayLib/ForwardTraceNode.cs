using System.Diagnostics;
using System.Reflection;
using System.Text;
using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.UIX;
using JetBrains.Annotations;

namespace KayLib;

[NodeName("Forward Trace")]
[Category("LogiX/Kay")]
[PublicAPI]
public sealed class ForwardTraceNode : LogixNode
{
    public readonly SyncRef<Text> Text;
    public readonly Impulse? Out;
    
    protected override string Label => null!;
    
    protected override void OnGenerateVisual(Slot root)
    {
        var ui = GenerateUI(root, 192f, 256f);
        ui.VerticalLayout(4f);
        ui.Style.MinHeight = 32f;
        var localeString = (LocaleString) "No Trace";
        Text.Target = ui.Text(in localeString, alignment: Alignment.TopLeft);
        Text.Target.AutoSize = false;
        Text.Target.Size.Value = 16.0f;
    }

    [ImpulseTarget]
    public void DoTrace()
    {
        if (Text.Target == null || Out is null)
            return;

        var target = Out.TargetNode;
        var trace = new ForwardTrace();
        var recovery = Instrument(target, trace);
        trace.AddEntry(target);
        Out?.Trigger();
        DeInstrument(target, recovery);
        
        var builder = new StringBuilder();

        var idx =1;
        
        foreach (var entry in trace.TraceEntries)
        {
            builder.Append($"{idx:D2}: {entry}\n");

            idx++;
        }
        
        Text.Target.Content.Value = builder.ToString();
    }

    public List<Action>? Instrument(LogixNode node, ForwardTrace trace)
    {
        var targets = node.GetSyncMembers<Impulse>();
        if (targets is null)
            return null;

        var recoveryData = targets.Select(x => x.Target).ToList();

        var field = typeof(SyncDelegate<Action>).GetField("_target",
            BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);

        foreach (var t in targets)
        {
            if (t is null)
                continue;
            
            var oldTarget = t.Target;
            if (oldTarget is null)
                continue;
            var oldTargetNode = t.TargetNode;
            if (oldTargetNode is null)
                continue;
            
            //TODO: Handle a few special nodes that do their own dispatch.
            
            Delegate d = () =>
            {
                Instrument(oldTargetNode, trace);
                trace.AddEntry(oldTargetNode);
                oldTarget();
                DeInstrument(oldTargetNode, recoveryData);
            };
           
            // This is just awful. Please never do this.
            field.SetValue(t, d);
            
        }

        return recoveryData;
    }

    public void DeInstrument(LogixNode node, List<Action>? actions)
    {
        if (actions is null)
            return;
        
        var targets = node.GetSyncMembers<Impulse>();

        foreach (var t in targets)
        {
            t.Target = actions.TakeFirst();
        }
    }
}

public sealed class ForwardTrace
{
    public List<string> TraceEntries = new();

    public void AddEntry(LogixNode node)
    {
        TraceEntries.Add($"N_{node.Name}");
    }
}