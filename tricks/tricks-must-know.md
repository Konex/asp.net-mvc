C# tricks every C# dev should know!
===========
**Extension methods**

	public static class IEnumerableUtils
	{
		public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach(T item in collection)
				action(item);
		}
	}
	
	public static object DynamicMap(this object source, object dest)
	{
		Mapper.DynamicMap(source, dest);
		return dest;
	}


     
