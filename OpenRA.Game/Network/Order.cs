#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA
{
	public abstract class CustomOrder : Order
	{
		[ObjectCreator.UseCtorAttribute]
		protected CustomOrder()
		{

		}

		protected CustomOrder(string orderString, Actor subject)
		{
			Init(orderString, subject);
		}

		internal void Init(string orderString, Actor subject)
		{
			OrderString = orderString;
			Subject = subject;
		}

		public abstract void OnSerialize(BinaryWriter w);
		public abstract bool OnDeserialize(World world, BinaryReader r);
	}

	public class Order
	{
		public string OrderString { get; protected set; }
		public Actor Subject { get; protected set; }
		public Actor TargetActor { get; protected set; }
		public int2 TargetLocation { get; protected set; }
		public string TargetString { get; protected set; }
		public bool Queued { get; protected set; }
		public bool IsImmediate { get; protected set; }
		public string OrderClass { get; protected set; }

		public Player Player { get { return Subject.Owner; } }

		internal Order()
		{
			this.OrderClass = GetType().Name;
		}

		public Order(string orderString, Actor subject, 
			Actor targetActor, int2 targetLocation, string targetString, bool queued)
		{
			this.OrderString = orderString;
			this.Subject = subject;
			this.TargetActor = targetActor;
			this.TargetLocation = targetLocation;
			this.TargetString = targetString;
			this.Queued = queued;
			this.OrderClass = GetType().Name;
		}

		public Order(string orderString, Actor subject, bool queued) 
			: this(orderString, subject, null, int2.Zero, null, queued) { }
		public Order(string orderString, Actor subject, Actor targetActor, bool queued)
			: this(orderString, subject, targetActor, int2.Zero, null, queued) { }
		public Order(string orderString, Actor subject, int2 targetLocation, bool queued)
			: this(orderString, subject, null, targetLocation, null, queued) { }
		public Order(string orderString, Actor subject, string targetString, bool queued)
			: this(orderString, subject, null, int2.Zero, targetString, queued) { }
		public Order(string orderString, Actor subject, Actor targetActor, int2 targetLocation, bool queued)
			: this(orderString, subject, targetActor, targetLocation, null, queued) { }
		public Order(string orderString, Actor subject, Actor targetActor, string targetString, bool queued)
			: this(orderString, subject, targetActor, int2.Zero, targetString, queued) { }
		public Order(string orderString, Actor subject, int2 targetLocation, string targetString, bool queued)
			: this(orderString, subject, null, targetLocation, targetString, queued) { }

		public byte[] Serialize()
		{
			var ret = new MemoryStream();
			var w = new BinaryWriter(ret);

			if (IsImmediate)		/* chat, whatever */
			{
				w.Write((byte)0xfe);
				w.Write(OrderString);
				w.Write(TargetString);
				return ret.ToArray();
			}

			var corder = this as CustomOrder;

			if (corder != null)
			{
				// Custom order : 0xFC
				w.Write((byte)0xFC);
				w.Write(OrderString);
				w.Write(UIntFromActor(Subject));
				w.Write(OrderClass);

				var retCustom = new MemoryStream();
				var wCustom = new BinaryWriter(retCustom);

				corder.OnSerialize(wCustom);
				var bCustom = retCustom.ToArray();
				w.Write((uint)bCustom.Length);
				w.Write(bCustom);
				return ret.ToArray();
			}

			// Regular order : 0xFF
			
			// Format:
			//		u8    : orderID.
			//		            0xFF: Full serialized order.
			//		varies: rest of order.

			w.Write((byte)0xFF);
			w.Write(OrderString);
			w.Write(UIntFromActor(Subject));
			Serialize(w);
			return ret.ToArray();
		}

		protected void Serialize(BinaryWriter w)
		{
			w.Write(UIntFromActor(TargetActor));
			w.Write(TargetLocation.X);
			w.Write(TargetLocation.Y);
			w.Write(TargetString != null);
			if (TargetString != null)
				w.Write(TargetString);
			w.Write(Queued);
		}

		public void SerializeDefault(BinaryWriter w)
		{
			Serialize(w);
		}
		public bool DeserializeDefault(World world, BinaryReader r)
		{
			var targetActorId = r.ReadUInt32();
			TargetLocation = new int2(r.ReadInt32(), r.ReadInt32());
			if (r.ReadBoolean())
				TargetString = r.ReadString();
			Queued = r.ReadBoolean();
			Actor targetActor;
			if (!TryGetActorFromUInt(world, targetActorId, out targetActor))
				return false;

			TargetActor = targetActor;

			return true;
		}

		public static Order Deserialize(World world, BinaryReader r)
		{
			switch (r.ReadByte())
			{
				case 0xFF: // regular order
					{
						var order = r.ReadString();
						var subjectId = r.ReadUInt32();
						var targetActorId = r.ReadUInt32();
						var targetLocation = new int2(r.ReadInt32(), 0);
						targetLocation.Y = r.ReadInt32();
						var targetString = null as string;
						if (r.ReadBoolean())
							targetString = r.ReadString();
						var queued = r.ReadBoolean();

						Actor subject, targetActor;
						if (!TryGetActorFromUInt(world, subjectId, out subject) || !TryGetActorFromUInt(world, targetActorId, out targetActor))
							return null;

						return new Order(order, subject, targetActor, targetLocation, targetString, queued);
					}

				case 0xFC: // custom order
					{
						var order = r.ReadString();
						var subjectId = r.ReadUInt32();
						var orderClass = r.ReadString();
						uint packSize = r.ReadUInt32();

						Actor subject;
						if (!TryGetActorFromUInt(world, subjectId, out subject))
							return null;

						var o = Game.CreateObject<CustomOrder>(orderClass);
						o.Init(order, subject);

						return o.OnDeserialize(world, r) ? o : null;
					}

				case 0xfe:
					{
						var name = r.ReadString();
						var data = r.ReadString();

						return new Order( name, null, data, false ) { IsImmediate = true };
					}

				default:
					throw new NotImplementedException();
			}
		}

		public override string ToString()
		{
			return ("OrderString: \"{0}\" \n\t Subject: \"{1}\". \n\t TargetActor: \"{2}\" \n\t TargetLocation: {3}." +
				"\n\t TargetString: \"{4}\".\n\t IsImmediate: {5}.\n\t Player(PlayerName): {6}\n").F(
				OrderString, Subject, TargetActor != null ? TargetActor.Info.Name : null , TargetLocation, TargetString, IsImmediate, Player != null ? Player.PlayerName : null);
		}

		protected static uint UIntFromActor(Actor a)
		{
			if (a == null) return 0xffffffff;
			return a.ActorID;
		}

		protected static bool TryGetActorFromUInt(World world, uint aID, out Actor ret )
		{
			if( aID == 0xFFFFFFFF )
			{
				ret = null;
				return true;
			}
			else
			{
				foreach( var a in world.Actors.Where( x => x.ActorID == aID ) )
				{
					ret = a;
					return true;
				}
				ret = null;
				return false;
			}
		}

		// Named constructors for Orders.
		// Now that Orders are resolved by individual Actors, these are weird; you unpack orders manually, but not pack them.
		public static Order Chat(string text)
		{
			return new Order("Chat", null, text, false) { IsImmediate = true };
		}

		public static Order TeamChat(string text)
		{
			return new Order("TeamChat", null, text, false) { IsImmediate = true };
		}
		
		public static Order Command(string text)
		{
			return new Order("Command", null, text, false) { IsImmediate = true };	
		}

		public static Order StartProduction(Actor subject, string item, int count)
		{
			return new Order("StartProduction", subject, new int2( count, 0 ), item, false );
		}

		public static Order PauseProduction(Actor subject, string item, bool pause)
		{
			return new Order("PauseProduction", subject, new int2( pause ? 1 : 0, 0 ), item, false);
		}

		public static Order CancelProduction(Actor subject, string item, int count)
		{
			return new Order("CancelProduction", subject, new int2( count, 0 ), item, false);
		}
	}
}
