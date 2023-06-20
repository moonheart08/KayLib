using System.Diagnostics;
using System.Reflection;
using System.Text;
using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.UIX;

namespace KayLib;

[NodeName("Trace")]
[Category("LogiX/Kay")]
public sealed class TraceNode : LogixNode
{
    public readonly SyncRef<Text> Text;
    
    protected override string Label => null!;
    
    protected override void OnGenerateVisual(Slot root)
    {
        var ui = GenerateUI(root, 192f, 128f);
        ui.VerticalLayout(4f);
        ui.Style.MinHeight = 32f;
        UniLog.Log($"Making visual for {root.World}, slot is in {Text.World}");
        var localeString = (LocaleString) "No Trace";
        Text.Target = ui.Text(in localeString, alignment: Alignment.TopLeft);
        Text.Target.AutoSize = false;
        Text.Target.Size.Value = 16.0f;
    }
    
    [ImpulseTarget]
    public void DoTrace()
    {
        if (Text.Target == null)
        {
            UniLog.Log("Missing target for trace!");
            return;
        }

        var trace = new StackTrace();
        var relevant = trace.GetFrames()
            .Select(x => x.GetMethod())
            .Where(x => x?.GetCustomAttribute(typeof(ImpulseTarget)) is not null)
            .Select(x => x!)
            .ToArray();

        var builder = new StringBuilder();

        var idx = relevant.Length;

        foreach (var method in relevant)
        {
            var nodeName = (NodeName?) method.DeclaringType!.GetCustomAttribute(typeof(NodeName));
            if (nodeName is null)
            {
                builder.Insert(0, $"{idx:D2}: M_{method.Name}\n");
            }
            else
            {
                builder.Insert(0, $"{idx:D2}: N_{nodeName.Name}\n");
            }

            idx--;
        }

        Text.Target.Content.Value = builder.ToString();
    }
}