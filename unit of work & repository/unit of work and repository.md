Unit of work and repository with Unity ioc
===========

In asp.net mvc + Entity Framework web development, I have seen people using dbContext directly in the controller (even in the view template).
I am personally not a big fan of this approach. It creates a lot of noise in the controller as it knows your data persistence logic. And really, controller should not need to know how we persist the data, all it should worry about is orchestrating the view and let someone else to worry about handling business logic, data persistence, etc. 

If you disagree with me it is okay just make sure you use the same dbContext per http request and dispose it when an http request finishes. 

If you are still with me so far that's great let's dive into the code.
Let's take a look at Repository.cs:
It is a generic repository and everything is pretty self-explanatory only two methods here deserve a bit of text.
    
    // IQueryable<T>
    public virtual IQueryable<T> GetDbQueryable(string includeProperties = "")
    {
        if (includeProperties == null) throw new ArgumentNullException("includeProperties");

        return includeProperties.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
            .Aggregate<string, IQueryable<T>>(DbSet, (current, includeProperty) => 
            current.Include(includeProperty));
    }

    // IEnumerable<T>
    public virtual IEnumerable<T> GetAll(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = "")
    {
        IQueryable<T> query = DbSet;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        foreach (var includeProperty in includeProperties.Split
            (new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            query = query.Include(includeProperty);
        }

        return orderBy != null ? orderBy(query).ToList() : query.ToList();
    }

Both methods will defer execution but:
The first virtual method returns an IQueryable type which means you can do Linq-to_Sql. So you can queryon the returned IQueryable further, the final query then will be executed in the db. On the other hand, the second method returns an IEnumerable, you can only do Linq-to-Object, meaning when you put further query on the returned IEnumerable, it actually goes to the db first, loads the result into memory then your second query will be executed in the memory.
I let them in the good hands and believe you know what suits your situation the best.
One another thing to note is the parameter 'inCludeProperties'. It is to deep load the relevant navigation properties. In your code you can do something like this:
    
    incld = "NavEntityName1" + "," + "NavEntityName2" + "NavEntityName3.NavEntityName4";

Notice the 'NavEntityName3.NavEntityName4'. This is how you get down to the deeper level of navigation properties. 

And that is our generic repository.
                