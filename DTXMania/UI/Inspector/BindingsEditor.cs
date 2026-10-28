using System.Globalization;
using System.Numerics;
using DTXMania.UI.Animation;
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

                DataBindingKind sourceKind = binding.SourceKindFor(element);

                ImGui.TableSetColumnIndex(2);
                DrawBindingRule(binding, kind, sourceKind, BindingClause.KeyType(element, binding.source),
                    PropertyAccessor.GetOrBuild(element.GetType(), binding.target)?.ValueType);

                if (kind != DataBindingKind.String && (binding.IsTest || sourceKind == DataBindingKind.Bool)
                    && ImGui.SmallButton("+ Condition"))
                {
                    MakeTestExplicit(binding);
                    binding.clauses ??= [];
                    binding.clauses.Add(new BindingClause());
                }

                ImGui.TableSetColumnIndex(3);
                if (ImGui.Button("x", new Vector2(square, square)))
                {
                    removeAt = i;
                }

                DrawClauseRows(element, binding, square);

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
    private static readonly string[] NumberRules = ["Value", "==", "!=", ">", ">=", "<", "<=", "Scale"];
    private static readonly string[] MatchNames = ["and", "or"];

    private static void DrawClauseRows(UIDrawable element, UIBinding binding, float square)
    {
        if (binding.clauses == null)
        {
            return;
        }

        int removeAt = -1;

        for (int c = 0; c < binding.clauses.Count; c++)
        {
            BindingClause clause = binding.clauses[c];
            ImGui.PushID(c + 1000);
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##match", MatchNames[(int)binding.match]))
            {
                for (int m = 0; m < MatchNames.Length; m++)
                {
                    if (ImGui.Selectable(MatchNames[m], m == (int)binding.match))
                    {
                        binding.match = (BindingMatch)m;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.TableSetColumnIndex(1);
            Inspector.DrawBindingDropdown("##clauseSource", ref clause.source, element, DataBindingKind.Bool, DataBindingKind.Number);

            ImGui.TableSetColumnIndex(2);
            DrawClauseRule(clause, clause.SourceKindFor(element), BindingClause.KeyType(element, clause.source));

            ImGui.TableSetColumnIndex(3);
            if (ImGui.Button("x", new Vector2(square, square)))
            {
                removeAt = c;
            }

            ImGui.PopID();
        }

        if (removeAt >= 0)
        {
            binding.clauses.RemoveAt(removeAt);
        }
    }

    private static void DrawClauseRule(BindingClause clause, DataBindingKind source, Type? sourceType)
    {
        //changing the source between a bool and a number resets the rule
        if (source == DataBindingKind.Bool && BindingClause.Compares(clause.condition))
        {
            clause.condition = BindingCondition.IsTrue;
        }
        else if (source == DataBindingKind.Number && !BindingClause.Compares(clause.condition))
        {
            clause.condition = BindingCondition.Equal;
        }

        if (source == DataBindingKind.Bool)
        {
            int current = clause.condition == BindingCondition.IsFalse ? 1 : 0;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##clauseRule", BoolRules[current]))
            {
                for (int r = 0; r < BoolRules.Length; r++)
                {
                    if (ImGui.Selectable(BoolRules[r], r == current))
                    {
                        clause.condition = r == 0 ? BindingCondition.IsTrue : BindingCondition.IsFalse;
                    }
                }

                ImGui.EndCombo();
            }

            return;
        }

        int index = (int)clause.condition;
        ImGui.SetNextItemWidth(ImGui.CalcTextSize("Value").X + ImGui.GetFrameHeight() * 1.5f);
        if (ImGui.BeginCombo("##clauseRule", NumberRules[index]))
        {
            for (int r = (int)BindingCondition.Equal; r <= (int)BindingCondition.LessOrEqual; r++)
            {
                if (ImGui.Selectable(NumberRules[r], r == index))
                {
                    clause.condition = (BindingCondition)r;
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1);
        DrawNumberInput("##clauseValue", ref clause.value, sourceType);
    }

    //ints and enums only allow whole values
    private static bool DrawNumberInput(string id, ref double value, Type? type)
    {
        if (type is { IsEnum: true })
        {
            string current = Enum.GetName(type, Enum.ToObject(type, (long)value)) ?? value.ToString(CultureInfo.InvariantCulture);
            bool picked = false;

            if (ImGui.BeginCombo(id, current))
            {
                foreach (object option in Enum.GetValues(type))
                {
                    double optionValue = Convert.ToDouble(option, CultureInfo.InvariantCulture);
                    if (ImGui.Selectable(option.ToString(), optionValue == value))
                    {
                        value = optionValue;
                        picked = true;
                    }
                }

                ImGui.EndCombo();
            }

            return picked;
        }

        if (type != null && DataFieldReflector.IsWholeNumber(type))
        {
            int whole = (int)Math.Round(value);
            if (!ImGui.InputInt(id, ref whole))
            {
                return false;
            }

            value = whole;
            return true;
        }

        float number = (float)value;
        if (!ImGui.InputFloat(id, ref number))
        {
            return false;
        }

        value = number;
        return true;
    }

    private static void MakeTestExplicit(UIBinding binding)
    {
        if (binding.condition == BindingCondition.None)
        {
            binding.condition = binding.invert ? BindingCondition.IsFalse : BindingCondition.IsTrue;
            binding.invert = false;
        }
    }

    private static void DrawBindingRule(UIBinding binding, DataBindingKind target, DataBindingKind source, Type? sourceType,
        Type? targetType)
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
            int scaleRule = NumberRules.Length - 1;
            int current = binding.condition == BindingCondition.Scale ? scaleRule
                : BindingClause.Compares(binding.condition) ? (int)binding.condition
                : 0;

            bool compares = current != 0 && current != scaleRule;
            ImGui.SetNextItemWidth(compares ? ImGui.CalcTextSize("Value").X + ImGui.GetFrameHeight() * 1.5f : -1);

            //Scale is only offered for number targets
            int offered = target == DataBindingKind.Number ? NumberRules.Length : scaleRule;

            if (ImGui.BeginCombo("##rule", NumberRules[current]))
            {
                for (int r = 0; r < offered; r++)
                {
                    if (ImGui.Selectable(NumberRules[r], r == current))
                    {
                        binding.condition = r == scaleRule ? BindingCondition.Scale : (BindingCondition)r;
                        binding.invert = false;
                    }
                }

                ImGui.EndCombo();
            }

            if (compares)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                DrawNumberInput("##value", ref binding.value, sourceType);
            }
        }

        if (target != DataBindingKind.Number || (binding.condition == BindingCondition.None && source != DataBindingKind.Bool))
        {
            return;
        }

        float half = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;
        float labelRoom = ImGui.CalcTextSize("False").X + ImGui.GetStyle().ItemInnerSpacing.X;

        if (binding.condition == BindingCondition.Scale)
        {
            ImGui.SetNextItemWidth(half - labelRoom);
            float scale = (float)binding.scale;
            if (ImGui.InputFloat("x##scale", ref scale))
            {
                binding.scale = scale;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(half - labelRoom);
            float offset = (float)binding.offset;
            if (ImGui.InputFloat("+##offset", ref offset))
            {
                binding.offset = offset;
            }

            return;
        }

        ImGui.SetNextItemWidth(half - labelRoom);
        if (DrawNumberInput("True##whenTrue", ref binding.whenTrue, targetType))
        {
            MakeTestExplicit(binding);
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(half - labelRoom);
        if (DrawNumberInput("False##whenFalse", ref binding.whenFalse, targetType))
        {
            MakeTestExplicit(binding);
        }
    }
}
