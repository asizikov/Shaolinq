// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("Sqlite")]
	public class TransactionTests
		: BaseTests
	{
		public TransactionTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Create_Object()
		{
			using (var scope = new TransactionScope())
			{
				var school = model.Schools.NewDataAccessObject();
				
				school.Name = "Kung Fu School";

				var student = model.Students.NewDataAccessObject();

				student.Firstname = "Bruce";
				student.Lastname = "Lee";
				student.School = school;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = model.Students.First(c => c.Firstname == "Bruce");

				Assert.AreEqual("Bruce Lee", student.Fullname);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_And_Abort()
		{
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.NewDataAccessObject();
				var student = school.Students.NewDataAccessObject();

				student.Firstname = "StudentThatShouldNotExist";
			}

			using (var scope = new TransactionScope())
			{
				Assert.Catch<InvalidOperationException>(() => model.Students.First(c => c.Firstname == "StudentThatShouldNotExist"));
			}
		}

		[Test]
		public void Test_Create_Object_And_Flush_Then_Abort()
		{
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.NewDataAccessObject();
				var student = school.Students.NewDataAccessObject();

				student.Firstname = "StudentThatShouldNotExist";

				scope.Flush(model);

				Assert.IsNotNull(model.Students.FirstOrDefault(c => c.Firstname == "StudentThatShouldNotExist"));
			}

			using (var scope = new TransactionScope())
			{
				Assert.Catch<InvalidOperationException>(() => model.Students.First(c => c.Firstname == "StudentThatShouldNotExist"));
			}
		}
	}
}