﻿// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Transactions;
using NUnit.Framework;
using Shaolinq.Tests.TestModel;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("Sqlite")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	public class TypeTests
		: BaseTests<TestDataAccessModel>
	{
		private readonly int floatSignificantFigures = 7;
		private readonly DateTime MinDatetime = DateTime.MinValue;
		private readonly DateTime MaxDateTime = DateTime.MaxValue;
		private readonly TimeSpan timespanEpsilon = TimeSpan.FromSeconds(1);
		private readonly float floatEpsilon = float.Epsilon;
		private readonly float floatMinValue = float.MinValue;
		private readonly float floatMaxValue = float.MaxValue;
		private readonly double doubleEpsilon = double.Epsilon;
		private readonly double doubleMinValue = double.MinValue;
		private readonly double doubleMaxValue = double.MaxValue;
		
		private static TimeSpan Abs(TimeSpan timeSpan)
		{
			if (timeSpan.TotalMilliseconds < 0)
			{
				return TimeSpan.FromMilliseconds(timeSpan.TotalMilliseconds * -1);
			}

			return timeSpan;
		}

		public TypeTests(string providerName)
			: base(providerName)
		{
			if (providerName == "MySql")
			{
				floatSignificantFigures = 6;

				MaxDateTime -= TimeSpan.FromSeconds(1);
			}
			else if (providerName.StartsWith("Sqlite") && useMonoData)
			{
				floatSignificantFigures = 3;
				floatEpsilon = 0;
				floatMinValue = float.MinValue + float.Epsilon;
				floatMaxValue = float.MaxValue - float.Epsilon;
				doubleEpsilon = 0;
				doubleMinValue = double.MinValue + double.Epsilon;
				doubleMaxValue = double.MaxValue - double.Epsilon;
			}
		}

		[Test]
		public void Test_Min_Values()
		{
			var minDecimal = decimal.MinValue;

			//decimal.MinValue seems to be too large for dotConnect to handle
			if (this.ProviderName.StartsWith("Postgres.DotConnect"))
			{
				minDecimal = long.MinValue;
			}

			ExecuteTest(
				"test",
				Guid.NewGuid(),
				short.MinValue,
				int.MinValue,
				long.MinValue,
				ushort.MinValue,
				uint.MinValue,
				ulong.MinValue,
				minDecimal,
				(float) TruncateToSignificantDigits(floatMinValue, floatSignificantFigures), // .NET internally stores 9 significant figures, but only 7 are used externally
				doubleMinValue,
				false,
				Truncate(MinDatetime, TimeSpan.FromMilliseconds(1)),
				TimeSpan.MinValue,
				Sex.Male,
				null);
		}

		[Test]
		public void Test_Max_Values()
		{
			var maxDecimal = decimal.MaxValue;

			//decimal.MaxValue seems to be too large for dotConnect to handle
			if (this.ProviderName.StartsWith("Postgres.DotConnect"))
			{
				maxDecimal = long.MaxValue;
			}

			ExecuteTest(
				"test",
				Guid.NewGuid(),
				short.MaxValue,
				int.MaxValue,
				long.MaxValue,
				(ushort) short.MaxValue, // using signed max value as unsigned not supported in various databases
				(uint) int.MaxValue, // using signed max value as unsigned not supported in various databases
				(ulong) long.MaxValue, // using signed max value as unsigned not supported in various databases
				maxDecimal,
				(float) TruncateToSignificantDigits(floatMaxValue, floatSignificantFigures), // .NET internally stores 9 significant figures, but only 7 are used externally
				doubleMaxValue,
				true,
				Truncate(MaxDateTime, TimeSpan.FromMilliseconds(1)),
				TimeSpan.Zero,
				Sex.Female,
				Truncate(MaxDateTime, TimeSpan.FromMilliseconds(1)));
		}

		[Test]
		public void Test_Non_Integer_Values()
		{
			ExecuteTest(
				"test",
				Guid.Empty,
				987,
				987654,
				987654321,
				123,
				123456,
				123456789,
				123.456789m,
				123.456f,
				TruncateToSignificantDigits(987.654321, floatSignificantFigures),
				true,
				Truncate(DateTime.UtcNow, TimeSpan.FromMilliseconds(1)),
				TimeSpan.FromHours(24),
				Sex.Female,
				null);
		}

		[Test]
		public void Test_Small_Values()
		{
			ExecuteTest(
				"test",
				Guid.Empty,
				1,
				1,
				1,
				1,
				1,
				1,
				0.00000000000000000001m,
				floatEpsilon,
				doubleEpsilon,
				true,
				Truncate(DateTime.UtcNow, TimeSpan.FromMilliseconds(1)),
				TimeSpan.FromMilliseconds(1),
				Sex.Female,
				Truncate(DateTime.UtcNow, TimeSpan.FromMilliseconds(1)));
		}

		private void ExecuteTest(
			string @string,
			Guid guid,
			short @short,
			int @int,
			long @long,
			ushort @ushort,
			uint @uint,
			ulong @ulong,
			decimal @decimal,
			float @float,
			double @double,
			bool @bool,
			DateTime dateTime,
			TimeSpan timeSpan,
			Sex @enum,
			DateTime? nullableDateTime
		)
		{
			long dbId;

			using (var scope = new TransactionScope())
			{
				var subject = model.ObjectWithManyTypes.Create();

				subject.String = @string;
				subject.Guid = guid;
				subject.Short = @short;
				subject.Int = @int;
				subject.Long = @long;
				subject.UShort = @ushort;
				subject.UInt = @uint;
				subject.ULong = @ulong;
				subject.Decimal = @decimal;
				subject.Float = @float;
				subject.Double = @double;
				subject.Bool = @bool;
				subject.DateTime = dateTime;
				subject.TimeSpan = timeSpan;
				subject.Enum = @enum;
				subject.NullableDateTime = nullableDateTime;
				//subject.ByteArray = byteArray;

				scope.Flush(model);

				dbId = subject.Id;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var dbObj = model.ObjectWithManyTypes.Single(x => x.Id == dbId);

				Assert.That(dbObj.String, Is.EqualTo(@string));
				Assert.That(dbObj.Guid, Is.EqualTo(guid));
				Assert.That(dbObj.Short, Is.EqualTo(@short));
				Assert.That(dbObj.Int, Is.EqualTo(@int));
				Assert.That(dbObj.Long, Is.EqualTo(@long));
				Assert.That(dbObj.UShort, Is.EqualTo(@ushort));
				Assert.That(dbObj.UInt, Is.EqualTo(@uint));
				Assert.That(dbObj.ULong, Is.EqualTo(@ulong));
				Assert.That(dbObj.Decimal, Is.EqualTo(@decimal));
				Assert.That(dbObj.Float, Is.EqualTo(@float));
				Assert.That(dbObj.Double, Is.EqualTo(@double));
				Assert.That(dbObj.Bool, Is.EqualTo(@bool));
				AssertDateTime(dbObj.DateTime, dateTime);
				Assert.That(Abs(dbObj.TimeSpan - timeSpan), Is.LessThan(timespanEpsilon));
				Assert.That(dbObj.Enum, Is.EqualTo(@enum));
				AssertNullable(dbObj.NullableDateTime, nullableDateTime, AssertDateTime);
				// Assert.That(dbObj.ByteArray, Is.EqualTo(byteArray));
			}
		}

		private static void AssertNullable<T>(T? nullable1, T? nullable2, Action<T, T> assertUnderlying) where T : struct
		{
			Assert.That(nullable1.HasValue, Is.EqualTo(nullable2.HasValue));
			if (nullable1.HasValue && nullable2.HasValue)
			{
				assertUnderlying(nullable1.Value, nullable2.Value);
			}
			else
			{
				Assert.That(nullable1, Is.Null);
				Assert.That(nullable2, Is.Null);
			}
		}

		private void AssertDateTime(DateTime dateTime1, DateTime dateTime2)
		{
			Assert.That(Abs(dateTime1.ToUniversalTime() - dateTime2.ToUniversalTime()), Is.LessThan(timespanEpsilon));
		}

		private static DateTime Truncate(DateTime dateTime, TimeSpan timeSpan)
		{
			return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
		}

		private static double TruncateToSignificantDigits(double d, int digits)
		{
			if (d == 0)
				return 0;

			var scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1 - digits);
			return scale * Math.Truncate(d / scale);
		}
	}
}