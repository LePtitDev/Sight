namespace Sight.Serialization.Schemas;

/// <summary>
/// Built-in <c>core</c> meta-schema, version 1 (binary specification §4.5)
/// </summary>
internal static class CoreSchema
{
    /// <summary>
    /// Value of <c>core.Kind</c> for a reference to a type alias (only in <c>TypeRef</c>, never a <see cref="TypeKind"/>)
    /// </summary>
    public const long AliasKind = 20;

    static CoreSchema()
    {
        var schema = new Schema("core", 1);
        Kind = schema.AddEnum("Kind", TypeKind.U8);
        for (var kind = TypeKind.Null; kind <= TypeKind.Any; kind++)
            Kind.AddValue(kind.ToString(), (long)kind);

        Kind.AddValue("Alias", AliasKind);

        SchemaRef = schema.AddStruct("SchemaRef");
        TypeRef = schema.AddStruct("TypeRef");
        TypeDef = schema.AddStruct("TypeDef", isAbstract: true);
        StructDef = schema.AddStruct("StructDef", TypeDef);
        EnumDef = schema.AddStruct("EnumDef", TypeDef);
        EnumValue = schema.AddStruct("EnumValue");
        Field = schema.AddStruct("Field");
        SchemaType = schema.AddStruct("Schema");
        AliasDef = schema.AddStruct("AliasDef", TypeDef);

        SchemaRef.AddField("name", TypeReference.String);
        SchemaRef.AddField("version", TypeReference.U32);
        SchemaRef.AddField("hash", TypeReference.U64.AsFixed());

        TypeRef.AddField("kind", TypeReference.Of(Kind));
        TypeRef.AddField("flags", TypeReference.U8);
        TypeRef.AddField("index", TypeReference.U32.AsNullable());
        TypeRef.AddField("args", TypeReference.List(TypeReference.Of(TypeRef)));

        TypeDef.AddField("name", TypeReference.String);

        StructDef.AddField("base", TypeReference.U32.AsNullable());
        StructDef.AddField("isAbstract", TypeReference.Bool);
        StructDef.AddField("fields", TypeReference.List(TypeReference.Of(Field)));
        StructDef.AddField("isPositional", TypeReference.Bool, false);

        EnumDef.AddField("underlying", TypeReference.Of(Kind));
        EnumDef.AddField("values", TypeReference.List(TypeReference.Of(EnumValue)));

        EnumValue.AddField("name", TypeReference.String);
        EnumValue.AddField("value", TypeReference.S64);

        Field.AddField("name", TypeReference.String);
        Field.AddField("type", TypeReference.Of(TypeRef));
        Field.AddField("aliases", TypeReference.List(TypeReference.String));
        Field.AddField("default", TypeReference.Any);

        AliasDef.AddField("target", TypeReference.Of(TypeRef));

        SchemaType.AddField("name", TypeReference.String);
        SchemaType.AddField("version", TypeReference.U32);
        SchemaType.AddField("imports", TypeReference.List(TypeReference.Of(SchemaRef)));
        SchemaType.AddField("types", TypeReference.List(TypeReference.Of(TypeDef)));

        schema.Validate();
        Instance = schema;
        Table = new TypeTable(schema.Closure);
    }

    public static Schema Instance { get; }

    public static TypeTable Table { get; }

    public static EnumDefinition Kind { get; }

    public static StructDefinition SchemaRef { get; }

    public static StructDefinition TypeRef { get; }

    public static StructDefinition TypeDef { get; }

    public static StructDefinition StructDef { get; }

    public static StructDefinition EnumDef { get; }

    public static StructDefinition EnumValue { get; }

    public static StructDefinition Field { get; }

    public static StructDefinition SchemaType { get; }

    public static StructDefinition AliasDef { get; }
}
