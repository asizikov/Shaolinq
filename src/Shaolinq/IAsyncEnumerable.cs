﻿// Copyright (c) 2007-2018 Thong Nguyen (tumtumtum@gmail.com)

using System.Collections.Generic;

namespace Shaolinq
{
	public interface IAsyncEnumerable<out T>
		: IEnumerable<T>
	{
		IAsyncEnumerator<T> GetAsyncEnumerator();
	}
}
