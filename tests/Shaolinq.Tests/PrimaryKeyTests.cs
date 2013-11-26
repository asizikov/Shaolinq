// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("Sqlite")]
	public class PrimaryKeyTests
		: BaseTests
	{
		public PrimaryKeyTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Create_Object_With_Guid_AutoIncrement_PrimaryKey_And_Set_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithGuidAutoIncrementPrimaryKeys.NewDataAccessObject();

				// Should not be able to set AutoIncrement property

				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj.Id = Guid.NewGuid());

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Guid_AutoIncrement_PrimaryKey_And_Get_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithGuidAutoIncrementPrimaryKeys.NewDataAccessObject();

				// AutoIncrement Guid  properties are set immediately

				Assert.IsTrue(obj.Id != Guid.Empty);
				
				scope.Flush(model);

				Assert.IsTrue(obj.Id != Guid.Empty);

				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj.Id = Guid.NewGuid());

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Guid_Non_AutoIncrement_PrimaryKey_And_Dont_Set_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj1 = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.NewDataAccessObject();

				obj1.Id = Guid.NewGuid();

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Guid_Non_AutoIncrement_PrimaryKey_And_Set_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.NewDataAccessObject();

				obj.Id = Guid.NewGuid();

				// Should not be able to set primary key twice

				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj.Id = Guid.NewGuid());

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Guid_Non_AutoIncrement_PrimaryKey_And_Dont_Set_PrimaryKey_Multiple()
		{
			Assert.Catch<TransactionAbortedException>(() =>
			{
				using (var scope = new TransactionScope())
				{
					var obj1 = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.NewDataAccessObject();
					var obj2 = model.ObjectWithGuidNonAutoIncrementPrimaryKeys.NewDataAccessObject();

					// Both objects will have same primary key (Guid.Empty)

					scope.Complete();
				}
			});
		}
		
		[Test]
		public void Test_Create_Object_With_Long_AutoIncrement_PrimaryKey_And_Set_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithLongAutoIncrementPrimaryKeys.NewDataAccessObject();

				// Should not be able to set AutoIncrement properties

				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj.Id = 1);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Long_AutoIncrement_PrimaryKey_And_Get_PrimaryKey()
		{
			using (var scope = new TransactionScope())
			{
				var obj1 = model.ObjectWithLongAutoIncrementPrimaryKeys.NewDataAccessObject();
				var obj2 = model.ObjectWithLongAutoIncrementPrimaryKeys.NewDataAccessObject();

				scope.Flush(model);

				Assert.AreEqual(1, obj1.Id);
				Assert.AreEqual(2, obj2.Id);

				// Should not be able to set AutoIncrement properties
				
				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj1.Id = 10);
				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj2.Id = 20);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Long_Non_AutoIncrement_PrimaryKey_And_Dont_Set_PrimaryKey()
		{
			var name = new StackTrace().GetFrame(0).GetMethod().Name;

			Assert.Throws<TransactionAbortedException>(() =>
			{
				using (var scope = new TransactionScope())
				{
					var obj = model.ObjectWithLongNonAutoIncrementPrimaryKeys.NewDataAccessObject();

					obj.Name = name;

					scope.Complete();
				}
			});
		}

		[Test]
		public void Test_Create_Object_With_Long_Non_AutoIncrement_PrimaryKey_And_Dont_Set_PrimaryKey_With_Default_Value()
		{
			var name = new StackTrace().GetFrame(0).GetMethod().Name;

			Assert.Throws<TransactionAbortedException>(() =>
			{
				using (var scope = new TransactionScope())
				{
					var obj = model.ObjectWithLongNonAutoIncrementPrimaryKeys.NewDataAccessObject();

					obj.Id = 0;
					obj.Name = name;

					scope.Complete();
				}
			});
		}


		[Test]
		public void Test_Create_Object_With_Long_Non_AutoIncrement_PrimaryKey_And_Set_PrimaryKey()
		{
			var name = new StackTrace().GetFrame(0).GetMethod().Name;

			using (var scope = new TransactionScope())
			{
				var obj = model.ObjectWithLongNonAutoIncrementPrimaryKeys.NewDataAccessObject();

				obj.Id = 999;
				obj.Name = name;

				Assert.Throws<InvalidPrimaryKeyPropertyAccessException>(() => obj.Id = 1);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				Assert.AreEqual(1, this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.Count(c => c.Name == name));
				Assert.AreEqual(999, this.model.ObjectWithLongNonAutoIncrementPrimaryKeys.FirstOrDefault(c => c.Name == name).Id);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_With_Composite_Primary_Keys()
		{
			var secondaryKey = new StackTrace().GetFrame(0).GetMethod().Name;

			using (var scope = new TransactionScope())
			{
				var obj1 = this.model.ObjectWithCompositePrimaryKeys.NewDataAccessObject();

				Assert.Catch<InvalidPrimaryKeyPropertyAccessException>(() => Console.WriteLine(obj1.Id));
				
				obj1.Id = 1;
				obj1.SecondaryKey = secondaryKey;
				obj1.Name = "Obj1";

				var obj2 = this.model.ObjectWithCompositePrimaryKeys.NewDataAccessObject();
				obj2.Id = 2;
				obj2.SecondaryKey = secondaryKey;
				obj2.Name = "Obj2";

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var obj1 = this.model.ObjectWithCompositePrimaryKeys.Single(c => c.SecondaryKey == secondaryKey && c.Id == 1);

				Assert.AreEqual(1, obj1.Id);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var obj = this.model.ObjectWithCompositePrimaryKeys.ReferenceTo(new
				{
					Id = 1,
					SecondaryKey = secondaryKey
				});

				Assert.IsTrue(obj.IsDeflatedReference);

				Assert.AreEqual("Obj1", obj.Name);

				Assert.IsFalse(obj.IsDeflatedReference);

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var obj1 = this.model.ObjectWithCompositePrimaryKeys.ReferenceTo(new
				{
					Id = 1,
					SecondaryKey = secondaryKey
				});

				Assert.IsTrue(obj1.IsDeflatedReference);

				var objs = this.model.ObjectWithCompositePrimaryKeys.Where(c => c == obj1 && c.Name != "");

				Assert.AreEqual(1, objs.Count());
				Assert.AreEqual(1, objs.Single().Id);

				objs = this.model.ObjectWithCompositePrimaryKeys.Where(c => c.SecondaryKey == secondaryKey);

				Assert.That(objs.Count(), Is.GreaterThan(1));

				scope.Complete();
			}
		}
	}
}