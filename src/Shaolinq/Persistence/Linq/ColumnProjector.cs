﻿// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq
{
	public class ColumnProjector
		: SqlExpressionVisitor
	{
		private class Nominator
			: SqlExpressionVisitor
		{
			private bool isBlocked;
			private readonly HashSet<Expression> candidates;
			private readonly Func<Expression, bool> fnCanBeColumn;

			private Nominator(Func<Expression, bool> canBeColumn)
			{
				this.fnCanBeColumn = canBeColumn;
				candidates = new HashSet<Expression>();
				isBlocked = false;
			}

			public static HashSet<Expression> Nominate(Func<Expression, bool> canBeColumn, Expression expression)
			{
				var nominator = new Nominator(canBeColumn);
                
				nominator.Visit(expression);

				return nominator.candidates;
			}
            
			protected override Expression Visit(Expression expression)
			{
				if (expression != null)
				{
					var saveIsBlocked = isBlocked;

					isBlocked = false;

					if (expression.NodeType != (ExpressionType)SqlExpressionType.Subquery)
					{
						base.Visit(expression);
					}

					if (!isBlocked)
					{
						if (fnCanBeColumn(expression))
						{
							candidates.Add(expression);
						}
						else
						{
							isBlocked = true;
						}
					}

					isBlocked |= saveIsBlocked;
				}

				return expression;
			}
		}

		private int columnIndex; 
		private readonly string newAlias;
		private readonly string[] existingAliases;

		private readonly DataAccessModel dataAccessModel;
		private readonly HashSet<string> columnNames; 
		private readonly HashSet<Expression> candidates;
		private readonly List<SqlColumnDeclaration> columns;
		private readonly Dictionary<MemberInitExpression, SqlObjectReference> sqlObjectReferenceByMemberInit;
		private readonly Dictionary<SqlColumnExpression, SqlColumnExpression> mappedColumnExpressions;

		internal ColumnProjector(DataAccessModel dataAccessModel, Func<Expression, bool> canBeColumn, Expression expression, string newAlias, Dictionary<MemberInitExpression, SqlObjectReference> sqlObjectReferenceByMemberInit, params string[] existingAliases)
		{
			columnNames = new HashSet<string>();
			columns = new List<SqlColumnDeclaration>();
			mappedColumnExpressions = new Dictionary<SqlColumnExpression, SqlColumnExpression>();

			this.sqlObjectReferenceByMemberInit = sqlObjectReferenceByMemberInit;
			this.dataAccessModel = dataAccessModel;
			this.newAlias = newAlias;
			this.existingAliases = existingAliases;
			this.candidates = Nominator.Nominate(canBeColumn, expression);
		}

		public static ProjectedColumns ProjectColumns(DataAccessModel dataAccessModel, Func<Expression, bool> canBeColumn, Expression expression, string newAlias, Dictionary<MemberInitExpression, SqlObjectReference> sqlObjectReferenceByMemberInit, params string[] existingAliases)
		{
			var projector = new ColumnProjector(dataAccessModel, canBeColumn, expression, newAlias, sqlObjectReferenceByMemberInit, existingAliases);

			expression = projector.Visit(expression);

			return new ProjectedColumns(expression, projector.columns.AsReadOnly());
		}

		protected override Expression VisitMemberInit(MemberInitExpression expression)
		{
			var retval = base.VisitMemberInit(expression);
			var newMemberInit = retval as MemberInitExpression;

			if (retval != expression && newMemberInit != null)
			{
				if (this.sqlObjectReferenceByMemberInit.ContainsKey(expression))
				{
					var newBindings = new List<MemberBinding>();
					var typeDescriptor = this.dataAccessModel.GetTypeDescriptor(expression.Type);

					foreach (var propertyDescriptor in typeDescriptor.PrimaryKeyProperties)
					{
						var localPropertyDescriptor = propertyDescriptor;

						foreach (var memberAssignmentExpressionExpression in newMemberInit
							.Bindings
							.OfType<MemberAssignment>()
							.Select(c => c.Expression)
							.OfType<SqlColumnExpression>()
							.Where(c => c.Name == localPropertyDescriptor.PropertyName))
						{
							newBindings.Add(Expression.Bind(propertyDescriptor.PropertyInfo, memberAssignmentExpressionExpression));
						}
					}

					var objectReference = new SqlObjectReference(newMemberInit.Type, newBindings);

					this.sqlObjectReferenceByMemberInit[expression] = objectReference;
				}
			}

			return retval;
		}
        
		private Expression ProcessExpression(Expression expression)
		{
			if (expression.NodeType == (ExpressionType)SqlExpressionType.Column)
			{
				SqlColumnExpression mappedColumnExpression;

				var column = (SqlColumnExpression)expression;

				if (mappedColumnExpressions.TryGetValue(column, out mappedColumnExpression))
				{
					return mappedColumnExpression;
				}

				if (existingAliases.Contains(column.SelectAlias))
				{
					var columnName = GetUniqueColumnName(column.Name);

					columns.Add(new SqlColumnDeclaration(columnName, column));
					mappedColumnExpression = new SqlColumnExpression(column.Type, newAlias, columnName);

					mappedColumnExpressions[column] = mappedColumnExpression;
					columnNames.Add(columnName);

					return mappedColumnExpression;
				}

				// Must be referring to outer scope

				return column;
			}
			else
			{
				var columnName = GetNextColumnName();
				
				columns.Add(new SqlColumnDeclaration(columnName, expression));

				return new SqlColumnExpression(expression.Type, newAlias, columnName);
			}
		}

		protected override Expression Visit(Expression expression)
		{
			if (candidates.Contains(expression))
			{
				if (expression.NodeType == (ExpressionType)SqlExpressionType.ObjectReference)
				{
					/*
					var listExpression = (SqlObjectReference)expression;
					var newList = new List<Expression>();
					var newPropertyNames = new List<string>();

					foreach (var subExpression in listExpression.ExpressionsInOrder)
					{
						var processed = ProcessExpression(subExpression);

						newList.Add(processed);
						var key = listExpression.PropertyNamesByExpression[subExpression];

						newPropertyNames.Add(key);
					}

					return new SqlObjectOperand(listExpression.Type, newList, newPropertyNames);*/

					return base.Visit(expression);
				}
				else
				{
					return ProcessExpression(expression);
				}
			}
			else
			{
				return base.Visit(expression);
			}
		}

		private bool IsColumnNameInUse(string name)
		{
			return columnNames.Contains(name);
		}

		private string GetUniqueColumnName(string name)
		{
			var suffix = 1; 
			var baseName = name;

			while (IsColumnNameInUse(name))
			{
				name = baseName + (suffix++);
			}

			return name;
		}
        
		private string GetNextColumnName()
		{
			return GetUniqueColumnName("COL" + (columnIndex++));
		}
	}
}
