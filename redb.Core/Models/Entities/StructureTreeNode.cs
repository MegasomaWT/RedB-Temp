using redb.Core.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace redb.Core.Models.Entities
{
    /// <summary>
    /// 🌳 Узел дерева структур для иерархической навигации
    /// Решает проблему плоского поиска структур в SaveAsync
    /// </summary>
    public class StructureTreeNode
    {
        public IRedbStructure Structure { get; set; } = null!;
        public List<StructureTreeNode> Children { get; set; } = new();
        public StructureTreeNode? Parent { get; set; }
        
        /// <summary>
        /// 🔍 НАВИГАЦИЯ ПО ДЕРЕВУ
        /// </summary>
        public bool IsRoot => Parent == null;
        public bool IsLeaf => Children.Count == 0;
        public int Depth => Parent?.Depth + 1 ?? 0;
        public string Path => Parent != null ? $"{Parent.Path}.{Structure.Name}" : Structure.Name;
        
        /// <summary>
        /// 🔎 ПОИСК В ДЕРЕВЕ
        /// </summary>
        public StructureTreeNode? FindChild(string name)
        {
            return Children.FirstOrDefault(c => c.Structure.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        
        public StructureTreeNode? FindDescendant(long structureId)
        {
            if (Structure.Id == structureId) return this;
            
            foreach (var child in Children)
            {
                var found = child.FindDescendant(structureId);
                if (found != null) return found;
            }
            
            return null;
        }
        
        public StructureTreeNode? FindDescendantByName(string name)
        {
            if (Structure.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return this;
            
            foreach (var child in Children)
            {
                var found = child.FindDescendantByName(name);
                if (found != null) return found;
            }
            
            return null;
        }
        
        public List<StructureTreeNode> GetAllDescendants()
        {
            var result = new List<StructureTreeNode>();
            
            foreach (var child in Children)
            {
                result.Add(child);
                result.AddRange(child.GetAllDescendants());
            }
            
            return result;
        }
        
        public List<IRedbStructure> GetChildrenStructures()
        {
            return Children.Select(c => c.Structure).ToList();
        }
        
        public List<StructureTreeNode> GetLeafNodes()
        {
            var result = new List<StructureTreeNode>();
            
            if (IsLeaf)
            {
                result.Add(this);
            }
            else
            {
                foreach (var child in Children)
                {
                    result.AddRange(child.GetLeafNodes());
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 🚶‍♂️ ОБХОД ДЕРЕВА
        /// </summary>
        public void WalkTree(Action<StructureTreeNode> visitor)
        {
            visitor(this);
            foreach (var child in Children)
            {
                child.WalkTree(visitor);
            }
        }
        
        public void WalkTreeWithDepth(Action<StructureTreeNode, int> visitor, int currentDepth = 0)
        {
            visitor(this, currentDepth);
            foreach (var child in Children)
            {
                child.WalkTreeWithDepth(visitor, currentDepth + 1);
            }
        }
        
        /// <summary>
        /// 🔍 ПРОВЕРКИ ИЕРАРХИИ
        /// </summary>
        public bool HasChild(string name)
        {
            return Children.Any(c => c.Structure.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        
        public bool HasDescendant(long structureId)
        {
            return FindDescendant(structureId) != null;
        }
        
        public bool IsDescendantOf(StructureTreeNode ancestor)
        {
            var current = Parent;
            while (current != null)
            {
                if (current.Structure.Id == ancestor.Structure.Id)
                    return true;
                current = current.Parent;
            }
            return false;
        }
        
        /// <summary>
        /// 📊 ДИАГНОСТИКА
        /// </summary>
        public override string ToString()
        {
            var indent = new string(' ', Depth * 2);
            var arrayIndicator = Structure.IsArray == true ? "[]" : "";
            var childrenCount = Children.Count > 0 ? $" ({Children.Count} children)" : "";
            return $"{indent}{Structure.Name}{arrayIndicator}{childrenCount}";
        }
        
        public string ToDetailedString()
        {
            return $"Structure {Structure.Id}: {Path} [Type: {Structure.IdType}, Array: {Structure.IsArray}, Depth: {Depth}]";
        }
        
        /// <summary>
        /// 🖨️ Печать всего дерева для диагностики  
        /// </summary>
        public string PrintTree()
        {
            var result = new List<string>();
            WalkTreeWithDepth((node, depth) => 
            {
                var indent = new string(' ', depth * 2);
                var arrayIndicator = node.Structure.IsArray == true ? "[]" : "";
                result.Add($"{indent}└─ {node.Structure.Name}{arrayIndicator} (ID: {node.Structure.Id})");
            });
            return string.Join("\n", result);
        }
    }
    
    /// <summary>
    /// 🏗️ Строитель дерева структур из плоских списков
    /// </summary>
    public static class StructureTreeBuilder
    {
        /// <summary>
        /// Построение дерева из плоского списка структур
        /// </summary>
        public static List<StructureTreeNode> BuildFromFlat(List<IRedbStructure> flatStructures)
        {
            var allNodes = new Dictionary<long, StructureTreeNode>();
            var rootNodes = new List<StructureTreeNode>();
            
            // Создаем все узлы
            foreach (var structure in flatStructures)
            {
                var node = new StructureTreeNode { Structure = structure };
                allNodes[structure.Id] = node;
            }
            
            // Устанавливаем связи parent-child
            foreach (var node in allNodes.Values)
            {
                if (node.Structure.IdParent.HasValue)
                {
                    if (allNodes.TryGetValue(node.Structure.IdParent.Value, out var parentNode))
                    {
                        node.Parent = parentNode;
                        parentNode.Children.Add(node);
                    }
                }
                else
                {
                    rootNodes.Add(node);
                }
            }
            
            // Сортируем children по Order
            foreach (var node in allNodes.Values)
            {
                node.Children = node.Children
                    .OrderBy(c => c.Structure.Order ?? 0)
                    .ThenBy(c => c.Structure.Id)
                    .ToList();
            }
            
            // Сортируем корневые узлы
            return rootNodes
                .OrderBy(n => n.Structure.Order ?? 0)
                .ThenBy(n => n.Structure.Id)
                .ToList();
        }
        
        /// <summary>
        /// Поиск узла по пути (например "Address1.Details.Floor")
        /// </summary>
        public static StructureTreeNode? FindNodeByPath(List<StructureTreeNode> tree, string path)
        {
            var parts = path.Split('.');
            StructureTreeNode? current = null;
            
            // Ищем корневой узел
            current = tree.FirstOrDefault(n => n.Structure.Name.Equals(parts[0], StringComparison.OrdinalIgnoreCase));
            if (current == null) return null;
            
            // Идем по пути
            for (int i = 1; i < parts.Length; i++)
            {
                current = current.FindChild(parts[i]);
                if (current == null) return null;
            }
            
            return current;
        }
        
        /// <summary>
        /// Получение всех узлов дерева в плоском списке
        /// </summary>
        public static List<StructureTreeNode> FlattenTree(List<StructureTreeNode> tree)
        {
            var result = new List<StructureTreeNode>();
            
            foreach (var root in tree)
            {
                result.Add(root);
                result.AddRange(root.GetAllDescendants());
            }
            
            return result;
        }
        
        /// <summary>
        /// Диагностика дерева - поиск проблем
        /// </summary>
        public static TreeDiagnosticReport DiagnoseTree(List<StructureTreeNode> tree, Type? csharpType = null)
        {
            var report = new TreeDiagnosticReport();
            
            // Поиск потерянных узлов
            var allNodes = FlattenTree(tree);
            foreach (var node in allNodes)
            {
                if (node.Structure.IdParent.HasValue && node.Parent == null)
                {
                    report.OrphanedNodes.Add($"Structure {node.Structure.Id} ({node.Structure.Name}) has parent ID but no parent node");
                }
            }
            
            // Проверка соответствия C# типу
            if (csharpType != null)
            {
                var csharpProperties = csharpType.GetProperties().Select(p => p.Name).ToHashSet();
                
                foreach (var root in tree)
                {
                    if (!csharpProperties.Contains(root.Structure.Name))
                    {
                        report.ExcessiveStructures.Add($"Structure '{root.Structure.Name}' not found in C# type {csharpType.Name}");
                    }
                }
                
                foreach (var propertyName in csharpProperties)
                {
                    if (!tree.Any(n => n.Structure.Name == propertyName))
                    {
                        report.MissingStructures.Add($"C# property '{propertyName}' has no corresponding structure");
                    }
                }
            }
            
            return report;
        }
    }
    
    /// <summary>
    /// 📋 Отчет диагностики дерева структур
    /// </summary>
    public class TreeDiagnosticReport
    {
        public List<string> ExcessiveStructures { get; set; } = new();
        public List<string> MissingStructures { get; set; } = new(); 
        public List<string> OrphanedNodes { get; set; } = new();
        public List<string> TypeMismatches { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        
        public bool IsValid => !ExcessiveStructures.Any() && !MissingStructures.Any() && 
                              !OrphanedNodes.Any() && !TypeMismatches.Any();
                              
        public string Summary => $"Valid: {IsValid}, Issues: {ExcessiveStructures.Count + MissingStructures.Count + OrphanedNodes.Count + TypeMismatches.Count}";
    }
}
