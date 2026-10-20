using System.Numerics;
using DTXMania.UI.Drawable;
using DTXMania.UI.DynamicElements;
using Hexa.NET.ImGui;

namespace DTXMania.UI.Inspector;

public static class BindingsEditor
{
    public static void Draw(UIDrawable element)
    {
        if (!ImGui.CollapsingHeader($"Bindings ({element.bindings.Count})###bindings"))
        {
            return;
        }

        int removeAt = -1;

        if (element.bindings.Count > 0 && ImGui.BeginTable("bindings", 4,
                ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame))
        {
            float square = ImGui.GetFrameHeight();
            ImGui.TableSetupColumn("Target", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Source", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Rule", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("##remove", ImGuiTableColumnFlags.WidthFixed, square);
            ImGui.TableHeadersRow();

            for (int i = 0; i < element.bindings.Count; i++)
            {
                UIBinding binding = element.bindings[i];
                DataBindingKind kind = binding.KindFor(element);
                ImGui.PushID(i);
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                PathPicker.Draw("##target", ref binding.target, PathPicker.TargetsFor(element));

                ImGui.TableSetColumnIndex(1);

                bool missing = !binding.resolved && binding.source.Length > 0;
                if (missing)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.45f, 0.4f, 1.0f));
                }

                if (kind == DataBindingKind.String)
                {
                    Inspector.DrawBindingDropdown("##source", ref binding.source, element, kind);
                }
                else
                {
                    Inspector.DrawBindingDropdown("##source", ref binding.source, element, DataBindingKind.Bool, DataBindingKind.Number);
                }

                if (missing)
                {
                    ImGui.PopStyleColor();
                }

                ImGui.TableSetColumnIndex(2);
                DrawBindingRule(binding, kind, binding.SourceKindFor(element));

                ImGui.TableSetColumnIndex(3);
                if (ImGui.Button("x", new Vector2(square, square)))
                {
                    removeAt = i;
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
        }

        if (removeAt >= 0)
        {
            element.bindings.RemoveAt(removeAt);
        }

        if (ImGui.Button("Add Binding"))
        {
            element.bindings.Add(new UIBinding());
        }
    }

    private static readonly string[] BoolRules = ["Is True", "Is False"];
    private static readonly string[] NumberRules = ["Value", "==", "!=", ">", ">=", "<", "<="];

    private static void MakeTestExplicit(UIBinding binding)
    {
        if (binding.condition == BindingCondition.None)
        {
            binding.condition = binding.invert ? BindingCondition.IsFalse : BindingCondition.IsTrue;
            binding.invert = false;
        }
    }

    private static void DrawBindingRule(UIBinding binding, DataBindingKind target, DataBindingKind source)
    {
        if (target == DataBindingKind.String && binding.condition == BindingCondition.None)
        {
            return;
        }

        if (source == DataBindingKind.Bool)
        {
            bool isFalse = binding.condition == BindingCondition.IsFalse
                           || (binding.condition == BindingCondition.None && binding.invert);

            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##rule", BoolRules[isFalse ? 1 : 0]))
            {
                for (int r = 0; r < BoolRules.Length; r++)
                {
                    if (ImGui.Selectable(BoolRules[r], r == (isFalse ? 1 : 0)))
                    {
                        binding.condition = r == 0 ? BindingCondition.IsTrue : BindingCondition.IsFalse;
                        binding.invert = false;
                    }
                }

                ImGui.EndCombo();
            }
        }
        else
        {
            //Value, then the comparisons in BindingCondition's order
            int current = binding.condition is >= BindingCondition.Equal and <= BindingCondition.LessOrEqual
                ? (int)binding.condition
                : 0;

            bool compares = current != 0;
            ImGui.SetNextItemWidth(compares ? ImGui.CalcTextSize("Value").X + ImGui.GetFrameHeight() * 1.5f : -1);

            if (ImGui.BeginCombo("##rule", NumberRules[current]))
            {
                for (int r = 0; r < NumberRules.Length; r++)
                {
                    if (ImGui.Selectable(NumberRules[r], r == current))
                    {
                        binding.condition = (BindingCondition)r;
                        binding.invert = false;
                    }
                }

                ImGui.EndCombo();
            }

            if (compares)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                float value = (float)binding.value;
                if (ImGui.InputFloat("##value", ref value))
                {
                    binding.value = value;
                }
            }
        }

        if (target != DataBindingKind.Number || (binding.condition == BindingCondition.None && source != DataBindingKind.Bool))
        {
            return;
        }

        float half = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;
        float labelRoom = ImGui.CalcTextSize("False").X + ImGui.GetStyle().ItemInnerSpacing.X;

        ImGui.SetNextItemWidth(half - labelRoom);
        float whenTrue = (float)binding.whenTrue;
        if (ImGui.InputFloat("True##whenTrue", ref whenTrue))
        {
            binding.whenTrue = whenTrue;
            MakeTestExplicit(binding);
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(half - labelRoom);
        float whenFalse = (float)binding.whenFalse;
        if (ImGui.InputFloat("False##whenFalse", ref whenFalse))
        {
            binding.whenFalse = whenFalse;
            MakeTestExplicit(binding);
        }
    }
}
