using Unity.Netcode;
using System;

// Struktura sieciowa do przesy³ania pionka
[Serializable]
public struct NetworkArmyPiece : INetworkSerializable, IEquatable<NetworkArmyPiece>
{
	public PieceType type;
	public int x;
	public int y;

	public NetworkArmyPiece(PieceType t, int c, int r)
	{
		type = t;
		x = c;
		y = r;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref type);
		serializer.SerializeValue(ref x);
		serializer.SerializeValue(ref y);
	}

	public bool Equals(NetworkArmyPiece other)
	{
		return type == other.type && x == other.x && y == other.y;
	}
}