using FishNet.Serializing;

namespace Stats.FishNet
{
    public static class StatNumberSerializer
    {
        public static void WriteTInt(this Writer writer, TInt value) => writer.WriteInt32(value);
        public static TInt ReadTInt(this Reader reader) => reader.ReadInt32();

        public static void WriteTFloat(this Writer writer, TFloat value) => writer.WriteSingle(value);
        public static TFloat ReadTFloat(this Reader reader) => reader.ReadSingle();
    }
}