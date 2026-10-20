using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Sight.Serialization.Data;
using Sight.Serialization.Internal;
using Sight.Serialization.Schemas;

namespace Sight.Serialization.Text;

internal static partial class TextFormat
{
    public static string Write(DataDocument document, SerializationOptions options)
    {
        foreach (var schema in document.Schemas)
            schema.Validate();
        return new DocumentWriter(document, options).Write();
    }

    /// <summary>
    /// Canonical writer of text documents (text specification §8)
    /// </summary>
    private sealed class DocumentWriter
    {
        private readonly DataDocument _document;
        private readonly SerializationOptions _options;
        private readonly TypeTable _table;
        private readonly StringBuilder _builder = new();
        private readonly Dictionary<string, string> _aliases = new(StringComparer.Ordinal);
        private readonly Dictionary<DataStruct, string> _anchors = new(ReferenceComparer<DataStruct>.Instance);
        private readonly HashSet<DataStruct> _written = new(ReferenceComparer<DataStruct>.Instance);
        private readonly string _indent;

        public DocumentWriter(DataDocument document, SerializationOptions options)
        {
            _document = document;
            _options = options;
            _table = TypeTable.For(document.Schemas);
            _indent = new string(' ', Math.Max(0, options.Indent));
        }

        public string Write()
        {
            ComputeAnchors();
            WriteDirectives();
            _builder.Append('\n');
            WriteTopLevel(_document.Root);
            _builder.Append('\n');
            if (_document.Exports.Count > 0)
            {
                _builder.Append('\n');
                foreach (var export in _document.Exports)
                {
                    _builder.Append("@export ").Append(TextFormatting.FormatName(export.Key)).Append(" = ");
                    if (_written.Contains(export.Value))
                        _builder.Append('*').Append(_anchors[export.Value]);
                    else
                        WriteTopLevel(export.Value);

                    _builder.Append('\n');
                }
            }

            return _builder.ToString();
        }

        private void WriteDirectives()
        {
            _builder.Append("@sbdl ").Append(FormatVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');
            if (_document.Id.Length > 0)
                _builder.Append("@id ").Append(Names.IsQualifiedName(_document.Id) ? _document.Id : TextFormatting.FormatString(_document.Id)).Append('\n');

            if (_document.Version != 0)
                _builder.Append("@version ").Append(_document.Version.ToString(CultureInfo.InvariantCulture)).Append('\n');

            foreach (var entry in _document.Metadata)
                _builder.Append("@meta ").Append(TextFormatting.FormatName(entry.Key)).Append(" = ").Append(TextFormatting.FormatString(entry.Value)).Append('\n');

            switch (_document.SchemaMode)
            {
                case SchemaMode.Inline:
                    foreach (var schema in _table.Closure)
                    {
                        _builder.Append("@schema {\n");
                        SchemaPrinter.Append(_builder, schema, _indent, _indent, isFile: false);
                        _builder.Append("}\n");
                    }

                    break;
                case SchemaMode.Reference:
                    foreach (var schema in _document.Schemas)
                        _builder.Append("@schema ").Append(schema.Name).Append(' ').Append(schema.Version.ToString(CultureInfo.InvariantCulture)).Append('\n');

                    break;
            }

            var used = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < _document.Dependencies.Count; i++)
            {
                var dependency = _document.Dependencies[i];
                var last = dependency.Id.Substring(dependency.Id.LastIndexOf('.') + 1);
                var unique = Names.IsIdentifier(last) && _document.Dependencies.Count(x => x.Id.Substring(x.Id.LastIndexOf('.') + 1) == last) == 1;
                var alias = unique ? last : "d" + (i + 1).ToString(CultureInfo.InvariantCulture);
                while (!used.Add(alias))
                    alias = "d" + alias;

                _aliases[dependency.Id] = alias;
                var id = Names.IsQualifiedName(dependency.Id) ? dependency.Id : TextFormatting.FormatString(dependency.Id);
                _builder.Append("@dep ").Append(alias).Append(" = ").Append(id).Append(' ').Append(dependency.Version.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }
        }

        /// <summary>
        /// Anchors: objects referenced more than once (export positions included), named in order of first appearance
        /// </summary>
        private void ComputeAnchors()
        {
            var counts = new Dictionary<DataStruct, int>(ReferenceComparer<DataStruct>.Instance);
            var order = new List<DataStruct>();
            var visited = new HashSet<DataValue>(ReferenceComparer<DataValue>.Instance);
            var forced = new HashSet<DataStruct>(ReferenceComparer<DataStruct>.Instance);

            Visit(_document.Root, null);
            foreach (var export in _document.Exports)
                Count(export.Value);

            var next = 1;
            foreach (var value in order)
            {
                if (counts[value] > 1 || forced.Contains(value))
                    _anchors[value] = (next++).ToString(CultureInfo.InvariantCulture);
            }

            void Count(DataStruct value)
            {
                if (counts.TryGetValue(value, out var count))
                {
                    counts[value] = count + 1;
                    return;
                }

                counts[value] = 1;
                order.Add(value);
                Visit(value, null);
            }

            void Visit(DataValue value, TypeReference? type)
            {
                switch (value)
                {
                    case DataStruct data:
                        if (!visited.Add(data))
                            return;

                        var fields = data.Definition.AllFields;
                        for (var i = 0; i < fields.Count; i++)
                        {
                            var fieldValue = data.GetAt(i);
                            if (fieldValue != null)
                                VisitTyped(fieldValue, fields[i].Type);
                        }

                        break;
                    case DataList list:
                        foreach (var item in list)
                            VisitTyped(item, type?.ElementType);

                        break;
                    case DataMap map:
                        foreach (var entry in map)
                            VisitTyped(entry.Value, type?.ValueType);

                        break;
                    case DataReference reference:
                        forced.Add(reference.Target);
                        Count(reference.Target);
                        break;
                }
            }

            void VisitTyped(DataValue value, TypeReference? type)
            {
                if (type?.Kind == TypeKind.Ref && value is DataStruct target)
                    Count(target);
                else
                    Visit(value, type?.Kind == TypeKind.Any ? null : type);
            }
        }

        private void WriteTopLevel(DataStruct value)
        {
            if (_anchors.TryGetValue(value, out var anchor))
                _builder.Append('&').Append(anchor).Append(' ');

            _written.Add(value);
            WriteStruct(value, includePrefix: true, level: 0);
        }

        private void WriteValue(DataValue value, TypeReference type, int level)
        {
            if (value.IsNull)
            {
                _builder.Append("null");
                return;
            }

            switch (type.Kind)
            {
                case TypeKind.Enum:
                    _builder.Append(((DataEnum)DataConvert.CoerceScalar(value, type, "value")).Name);
                    break;
                case TypeKind.List:
                    WriteList((DataList)value, type.ElementType!, level);
                    break;
                case TypeKind.Map:
                    WriteMap((DataMap)value, type.KeyType!, type.ValueType!, level);
                    break;
                case TypeKind.Struct:
                    var data = (DataStruct)value;
                    WriteStruct(data, !ReferenceEquals(data.Definition, type.StructDefinition), level);
                    break;
                case TypeKind.Ref:
                    WriteRef(value as DataStruct ?? ((DataReference)value).Target, type.StructDefinition!, level);
                    break;
                case TypeKind.ExtRef:
                    WriteExtRef((DataExtRef)value);
                    break;
                case TypeKind.Any:
                    WriteAny(value, level);
                    break;
                default:
                    WriteScalar(DataConvert.CoerceScalar(value, type, "value"), level, withSuffix: false);
                    break;
            }
        }

        private void WriteScalar(DataValue value, int level, bool withSuffix)
        {
            if (value.Kind == TypeKind.String && value.AsString().IndexOf('\n') >= 0)
            {
                WriteMultiLineString(value.AsString(), level);
                return;
            }

            _builder.Append(TextFormatting.FormatScalar(value, withSuffix));
        }

        private void WriteMultiLineString(string value, int level)
        {
            var indentation = Indent(level + 1);
            _builder.Append("\"\"\"\n");
            foreach (var line in value.Split('\n'))
            {
                if (line.Length > 0)
                    _builder.Append(indentation).Append(EscapeMultiLine(line));

                _builder.Append('\n');
            }

            _builder.Append(indentation).Append("\"\"\"");
        }

        private static string EscapeMultiLine(string line)
        {
            var escaped = TextFormatting.FormatString(line);
            return escaped.Substring(1, escaped.Length - 2);
        }

        private void WriteRef(DataStruct target, StructDefinition declared, int level)
        {
            if (_written.Contains(target))
            {
                _builder.Append('*').Append(_anchors[target]);
                return;
            }

            if (_anchors.TryGetValue(target, out var anchor))
                _builder.Append('&').Append(anchor).Append(' ');

            _written.Add(target);
            WriteStruct(target, !ReferenceEquals(target.Definition, declared), level);
        }

        private void WriteExtRef(DataExtRef value)
        {
            if (!_aliases.TryGetValue(value.DependencyId, out var alias))
                throw new SerializationException($"Dependency '{value.DependencyId}' is not declared in the document");

            _builder.Append('<').Append(alias);
            if (value.Key.Length > 0)
                _builder.Append('#').Append(TextFormatting.FormatName(value.Key));

            _builder.Append('>');
        }

        private void WriteAny(DataValue value, int level)
        {
            switch (value)
            {
                case DataScalar:
                    WriteScalar(value, level, withSuffix: true);
                    break;
                case DataEnum data:
                    _builder.Append(TypeName(data.Definition)).Append('.').Append(data.Name);
                    break;
                case DataStruct data:
                    if (_anchors.ContainsKey(data))
                        throw new SerializationException($"Object of type '{data.TypeId}' is referenced elsewhere and must be stored as a reference in 'any'");

                    WriteStruct(data, includePrefix: true, level);
                    break;
                case DataReference reference:
                    if (_written.Contains(reference.Target))
                    {
                        _builder.Append('*').Append(_anchors[reference.Target]);
                        break;
                    }

                    _builder.Append('&').Append(_anchors[reference.Target]).Append(' ');
                    _written.Add(reference.Target);
                    WriteStruct(reference.Target, includePrefix: true, level);
                    break;
                case DataList list:
                    var elementType = list.ElementType ?? TypeReference.Any;
                    if (elementType.Kind == TypeKind.U8 && !elementType.IsNullable)
                    {
                        WriteList(list, elementType, level);
                        break;
                    }

                    if (elementType.Kind != TypeKind.Any)
                        _builder.Append(TypeText(TypeReference.List(elementType))).Append(' ');

                    WriteList(list, elementType, level);
                    break;
                case DataMap map:
                    var keyType = map.KeyType ?? TypeReference.String;
                    var valueType = map.ValueType ?? TypeReference.Any;
                    if (keyType.Kind != TypeKind.String || valueType.Kind != TypeKind.Any)
                        _builder.Append(TypeText(TypeReference.Map(keyType, valueType))).Append(' ');

                    WriteMap(map, keyType, valueType, level);
                    break;
                case DataExtRef extRef:
                    _builder.Append(TypeText(TypeReference.ExtRef(extRef.TargetType ?? throw new SerializationException("An external reference stored in 'any' requires a target type")))).Append(' ');
                    WriteExtRef(extRef);
                    break;
                default:
                    throw new SerializationException($"A value of kind {value.Kind} cannot be stored in 'any'");
            }
        }

        private void WriteList(DataList list, TypeReference elementType, int level)
        {
            if (elementType.Kind == TypeKind.U8 && !elementType.IsNullable)
            {
                var bytes = list.Select(x => (byte)DataConvert.CoerceScalar(x, elementType, "list").AsUInt64()).ToArray();
                _builder.Append("b64\"").Append(Convert.ToBase64String(bytes)).Append('"');
                return;
            }

            if (list.Count == 0)
            {
                _builder.Append("[]");
                return;
            }

            if (TryWriteInline(() =>
            {
                _builder.Append('[');
                for (var i = 0; i < list.Count; i++)
                {
                    if (i > 0)
                        _builder.Append(", ");

                    WriteValue(list[i], elementType, level + 1);
                }

                _builder.Append(']');
            }))
                return;

            _builder.Append("[\n");
            foreach (var item in list)
            {
                _builder.Append(Indent(level + 1));
                WriteValue(item, elementType, level + 1);
                _builder.Append('\n');
            }

            _builder.Append(Indent(level)).Append(']');
        }

        private void WriteMap(DataMap map, TypeReference keyType, TypeReference valueType, int level)
        {
            if (map.Count == 0)
            {
                _builder.Append("{}");
                return;
            }

            if (TryWriteInline(() =>
            {
                _builder.Append("{ ");
                var first = true;
                foreach (var entry in map)
                {
                    if (!first)
                        _builder.Append(", ");

                    first = false;
                    WriteKey(entry.Key, keyType);
                    _builder.Append(": ");
                    WriteValue(entry.Value, valueType, level + 1);
                }

                _builder.Append(" }");
            }))
                return;

            _builder.Append("{\n");
            foreach (var entry in map)
            {
                _builder.Append(Indent(level + 1));
                WriteKey(entry.Key, keyType);
                _builder.Append(": ");
                WriteValue(entry.Value, valueType, level + 1);
                _builder.Append('\n');
            }

            _builder.Append(Indent(level)).Append('}');
        }

        private void WriteKey(DataValue key, TypeReference keyType)
        {
            var value = DataConvert.CoerceScalar(key, keyType, "map key");
            _builder.Append(value is DataEnum data ? data.Name : TextFormatting.FormatScalar(value, withSuffix: false));
        }

        private void WriteStruct(DataStruct value, bool includePrefix, int level)
        {
            if (includePrefix)
                _builder.Append(TypeName(value.Definition)).Append(' ');

            var fields = new List<(FieldDefinition Field, DataValue Value)>();
            var definitions = value.Definition.AllFields;
            for (var i = 0; i < definitions.Count; i++)
            {
                var field = definitions[i];
                var fieldValue = value.GetAt(i) ?? DataValue.Null;
                if (!_options.WriteDefaults)
                {
                    if (fieldValue.IsNull && field.Type.IsNullable)
                        continue;

                    if (field.DefaultValue != null && !fieldValue.IsNull && DataConvert.CoerceScalar(fieldValue, field.Type, field.Name).Equals(field.DefaultValue))
                        continue;
                }
                else if (fieldValue.IsNull && field.DefaultValue != null)
                {
                    fieldValue = field.DefaultValue;
                }

                if (fieldValue.IsNull && !field.Type.IsNullable)
                    throw new SerializationException($"Required field '{field.Name}' of '{value.Definition.QualifiedName}' is not set");

                fields.Add((field, fieldValue));
            }

            if (fields.Count == 0)
            {
                _builder.Append("{}");
                return;
            }

            if (TryWriteInline(() =>
            {
                _builder.Append("{ ");
                for (var i = 0; i < fields.Count; i++)
                {
                    if (i > 0)
                        _builder.Append(", ");

                    _builder.Append(fields[i].Field.Name).Append(": ");
                    WriteValue(fields[i].Value, fields[i].Field.Type, level + 1);
                }

                _builder.Append(" }");
            }))
                return;

            _builder.Append("{\n");
            foreach (var (field, fieldValue) in fields)
            {
                _builder.Append(Indent(level + 1)).Append(field.Name).Append(": ");
                WriteValue(fieldValue, field.Type, level + 1);
                _builder.Append('\n');
            }

            _builder.Append(Indent(level)).Append('}');
        }

        /// <summary>
        /// Write a container on a single line if it fits and has no line break, otherwise restore the builder
        /// </summary>
        private bool TryWriteInline(Action write)
        {
            var start = _builder.Length;
            var lineStart = start;
            while (lineStart > 0 && _builder[lineStart - 1] != '\n')
                lineStart--;

            var written = new HashSet<DataStruct>(_written, ReferenceComparer<DataStruct>.Instance);
            write();
            var fits = _builder.Length - lineStart <= _options.MaxInlineWidth;
            if (fits)
            {
                for (var i = start; i < _builder.Length; i++)
                {
                    if (_builder[i] == '\n')
                    {
                        fits = false;
                        break;
                    }
                }
            }

            if (fits)
                return true;

            _builder.Length = start;
            _written.Clear();
            _written.UnionWith(written);
            return false;
        }

        private string TypeName(TypeDefinition definition)
        {
            try
            {
                if (ReferenceEquals(_table.FindByName(definition.Name), definition))
                    return definition.Name;
            }
            catch (SerializationException)
            {
                // Ambiguous short name
            }

            return definition.QualifiedName;
        }

        /// <summary>
        /// Type expression with short type names when they are unambiguous
        /// </summary>
        private string TypeText(TypeReference type)
        {
            switch (type.Kind)
            {
                case TypeKind.List:
                    return "list<" + TypeText(type.ElementType!) + ">" + (type.IsNullable ? "?" : string.Empty);
                case TypeKind.Map:
                    return "map<" + TypeText(type.KeyType!) + ", " + TypeText(type.ValueType!) + ">" + (type.IsNullable ? "?" : string.Empty);
                case TypeKind.Enum:
                case TypeKind.Struct:
                    return TypeName(type.Definition!) + (type.IsNullable ? "?" : string.Empty);
                case TypeKind.Ref:
                case TypeKind.ExtRef:
                    return type.Kind.ToKeyword() + "<" + TypeName(type.Definition!) + ">" + (type.IsNullable ? "?" : string.Empty);
                default:
                    return type.ToString();
            }
        }

        private string Indent(int level)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < level; i++)
                builder.Append(_indent);

            return builder.ToString();
        }
    }
}
