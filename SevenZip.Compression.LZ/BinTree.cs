using System;
using System.IO;

namespace SevenZip.Compression.LZ;

public class BinTree : InWindow, IMatchFinder, IInWindowStream
{
	private uint _cyclicBufferPos;

	private uint _cyclicBufferSize;

	private uint _matchMaxLen;

	private uint[] _son;

	private uint[] _hash;

	private uint _cutValue = 255u;

	private uint _hashMask;

	private uint _hashSizeSum;

	private bool HASH_ARRAY = true;

	private const uint kHash2Size = 1024u;

	private const uint kHash3Size = 65536u;

	private const uint kBT2HashSize = 65536u;

	private const uint kStartMaxLen = 1u;

	private const uint kHash3Offset = 1024u;

	private const uint kEmptyHashValue = 0u;

	private const uint kMaxValForNormalize = 2147483647u;

	private uint kNumHashDirectBytes;

	private uint kMinMatchCheck = 4u;

	private uint kFixHashSize = 66560u;

	public void SetType(int numHashBytes)
	{
		HASH_ARRAY = numHashBytes > 2;
		if (HASH_ARRAY)
		{
			kNumHashDirectBytes = 0u;
			kMinMatchCheck = 4u;
			kFixHashSize = 66560u;
		}
		else
		{
			kNumHashDirectBytes = 2u;
			kMinMatchCheck = 3u;
			kFixHashSize = 0u;
		}
	}

	public new void SetStream(Stream stream)
	{
		base.SetStream(stream);
	}

	public new void ReleaseStream()
	{
		base.ReleaseStream();
	}

	public new void Init()
	{
		base.Init();
		for (uint num = 0u; num < _hashSizeSum; num++)
		{
			_hash[num] = 0u;
		}
		_cyclicBufferPos = 0u;
		ReduceOffsets(-1);
	}

	public new void MovePos()
	{
		if (++_cyclicBufferPos >= _cyclicBufferSize)
		{
			_cyclicBufferPos = 0u;
		}
		base.MovePos();
		if (_pos == int.MaxValue)
		{
			Normalize();
		}
	}

	public new byte GetIndexByte(int index)
	{
		return base.GetIndexByte(index);
	}

	public new uint GetMatchLen(int index, uint distance, uint limit)
	{
		return base.GetMatchLen(index, distance, limit);
	}

	public new uint GetNumAvailableBytes()
	{
		return base.GetNumAvailableBytes();
	}

	public void Create(uint historySize, uint keepAddBufferBefore, uint matchMaxLen, uint keepAddBufferAfter)
	{
		if (historySize > 2147483391)
		{
			throw new Exception();
		}
		_cutValue = 16 + (matchMaxLen >> 1);
		uint keepSizeReserv = (historySize + keepAddBufferBefore + matchMaxLen + keepAddBufferAfter) / 2u + 256;
		Create(historySize + keepAddBufferBefore, matchMaxLen + keepAddBufferAfter, keepSizeReserv);
		_matchMaxLen = matchMaxLen;
		uint num = historySize + 1;
		if (_cyclicBufferSize != num)
		{
			_son = new uint[(_cyclicBufferSize = num) * 2];
		}
		uint num2 = 65536u;
		if (HASH_ARRAY)
		{
			num2 = historySize - 1;
			num2 |= num2 >> 1;
			num2 |= num2 >> 2;
			num2 |= num2 >> 4;
			num2 |= num2 >> 8;
			num2 >>= 1;
			num2 |= 0xFFFFu;
			if (num2 > 16777216)
			{
				num2 >>= 1;
			}
			_hashMask = num2;
			num2++;
			num2 += kFixHashSize;
		}
		if (num2 != _hashSizeSum)
		{
			_hash = new uint[_hashSizeSum = num2];
		}
	}

	public uint GetMatches(uint[] distances)
	{
		uint num;
		if (_pos + _matchMaxLen <= _streamPos)
		{
			num = _matchMaxLen;
		}
		else
		{
			num = _streamPos - _pos;
			if (num < kMinMatchCheck)
			{
				MovePos();
				return 0u;
			}
		}
		uint num2 = 0u;
		uint num3 = ((_pos > _cyclicBufferSize) ? (_pos - _cyclicBufferSize) : 0u);
		uint num4 = _bufferOffset + _pos;
		uint num5 = 1u;
		uint num6 = 0u;
		uint num7 = 0u;
		uint num10;
		if (HASH_ARRAY)
		{
			uint num8 = CRC.Table[_bufferBase[num4]] ^ _bufferBase[num4 + 1];
			num6 = num8 & 0x3FFu;
			int num9 = (int)num8 ^ (_bufferBase[num4 + 2] << 8);
			num7 = (uint)num9 & 0xFFFFu;
			num10 = ((uint)num9 ^ (CRC.Table[_bufferBase[num4 + 3]] << 5)) & _hashMask;
		}
		else
		{
			num10 = (uint)(_bufferBase[num4] ^ (_bufferBase[num4 + 1] << 8));
		}
		uint num11 = _hash[kFixHashSize + num10];
		if (HASH_ARRAY)
		{
			uint num12 = _hash[num6];
			uint num13 = _hash[1024 + num7];
			_hash[num6] = _pos;
			_hash[1024 + num7] = _pos;
			if (num12 > num3 && _bufferBase[_bufferOffset + num12] == _bufferBase[num4])
			{
				num5 = (distances[num2++] = 2u);
				distances[num2++] = _pos - num12 - 1;
			}
			if (num13 > num3 && _bufferBase[_bufferOffset + num13] == _bufferBase[num4])
			{
				if (num13 == num12)
				{
					num2 -= 2;
				}
				num5 = (distances[num2++] = 3u);
				distances[num2++] = _pos - num13 - 1;
				num12 = num13;
			}
			if (num2 != 0 && num12 == num11)
			{
				num2 -= 2;
				num5 = 1u;
			}
		}
		_hash[kFixHashSize + num10] = _pos;
		uint num14 = (_cyclicBufferPos << 1) + 1;
		uint num15 = _cyclicBufferPos << 1;
		uint val;
		uint val2 = (val = kNumHashDirectBytes);
		if (kNumHashDirectBytes != 0 && num11 > num3 && _bufferBase[_bufferOffset + num11 + kNumHashDirectBytes] != _bufferBase[num4 + kNumHashDirectBytes])
		{
			num5 = (distances[num2++] = kNumHashDirectBytes);
			distances[num2++] = _pos - num11 - 1;
		}
		uint cutValue = _cutValue;
		while (true)
		{
			if (num11 <= num3 || cutValue-- == 0)
			{
				_son[num14] = (_son[num15] = 0u);
				break;
			}
			uint num16 = _pos - num11;
			uint num17 = ((num16 <= _cyclicBufferPos) ? (_cyclicBufferPos - num16) : (_cyclicBufferPos - num16 + _cyclicBufferSize)) << 1;
			uint num18 = _bufferOffset + num11;
			uint num19 = Math.Min(val2, val);
			if (_bufferBase[num18 + num19] == _bufferBase[num4 + num19])
			{
				while (++num19 != num && _bufferBase[num18 + num19] == _bufferBase[num4 + num19])
				{
				}
				if (num5 < num19)
				{
					num5 = (distances[num2++] = num19);
					distances[num2++] = num16 - 1;
					if (num19 == num)
					{
						_son[num15] = _son[num17];
						_son[num14] = _son[num17 + 1];
						break;
					}
				}
			}
			if (_bufferBase[num18 + num19] < _bufferBase[num4 + num19])
			{
				_son[num15] = num11;
				num15 = num17 + 1;
				num11 = _son[num15];
				val = num19;
			}
			else
			{
				_son[num14] = num11;
				num14 = num17;
				num11 = _son[num14];
				val2 = num19;
			}
		}
		MovePos();
		return num2;
	}

	public void Skip(uint num)
	{
		do
		{
			uint num2;
			if (_pos + _matchMaxLen <= _streamPos)
			{
				num2 = _matchMaxLen;
			}
			else
			{
				num2 = _streamPos - _pos;
				if (num2 < kMinMatchCheck)
				{
					MovePos();
					continue;
				}
			}
			uint num3 = ((_pos > _cyclicBufferSize) ? (_pos - _cyclicBufferSize) : 0u);
			uint num4 = _bufferOffset + _pos;
			uint num9;
			if (HASH_ARRAY)
			{
				uint num5 = CRC.Table[_bufferBase[num4]] ^ _bufferBase[num4 + 1];
				uint num6 = num5 & 0x3FFu;
				_hash[num6] = _pos;
				int num7 = (int)num5 ^ (_bufferBase[num4 + 2] << 8);
				uint num8 = (uint)num7 & 0xFFFFu;
				_hash[1024 + num8] = _pos;
				num9 = ((uint)num7 ^ (CRC.Table[_bufferBase[num4 + 3]] << 5)) & _hashMask;
			}
			else
			{
				num9 = (uint)(_bufferBase[num4] ^ (_bufferBase[num4 + 1] << 8));
			}
			uint num10 = _hash[kFixHashSize + num9];
			_hash[kFixHashSize + num9] = _pos;
			uint num11 = (_cyclicBufferPos << 1) + 1;
			uint num12 = _cyclicBufferPos << 1;
			uint val;
			uint val2 = (val = kNumHashDirectBytes);
			uint cutValue = _cutValue;
			while (true)
			{
				if (num10 <= num3 || cutValue-- == 0)
				{
					_son[num11] = (_son[num12] = 0u);
					break;
				}
				uint num13 = _pos - num10;
				uint num14 = ((num13 <= _cyclicBufferPos) ? (_cyclicBufferPos - num13) : (_cyclicBufferPos - num13 + _cyclicBufferSize)) << 1;
				uint num15 = _bufferOffset + num10;
				uint num16 = Math.Min(val2, val);
				if (_bufferBase[num15 + num16] == _bufferBase[num4 + num16])
				{
					while (++num16 != num2 && _bufferBase[num15 + num16] == _bufferBase[num4 + num16])
					{
					}
					if (num16 == num2)
					{
						_son[num12] = _son[num14];
						_son[num11] = _son[num14 + 1];
						break;
					}
				}
				if (_bufferBase[num15 + num16] < _bufferBase[num4 + num16])
				{
					_son[num12] = num10;
					num12 = num14 + 1;
					num10 = _son[num12];
					val = num16;
				}
				else
				{
					_son[num11] = num10;
					num11 = num14;
					num10 = _son[num11];
					val2 = num16;
				}
			}
			MovePos();
		}
		while (--num != 0);
	}

	private void NormalizeLinks(uint[] items, uint numItems, uint subValue)
	{
		for (uint num = 0u; num < numItems; num++)
		{
			uint num2 = items[num];
			num2 = (items[num] = ((num2 > subValue) ? (num2 - subValue) : 0u));
		}
	}

	private void Normalize()
	{
		uint subValue = _pos - _cyclicBufferSize;
		NormalizeLinks(_son, _cyclicBufferSize * 2, subValue);
		NormalizeLinks(_hash, _hashSizeSum, subValue);
		ReduceOffsets((int)subValue);
	}

	public void SetCutValue(uint cutValue)
	{
		_cutValue = cutValue;
	}
}
