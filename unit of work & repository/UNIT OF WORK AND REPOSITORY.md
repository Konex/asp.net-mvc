Unit of work and repository with Unity ioc
===========

In asp.net mvc + Entity Framework web development, I have seen people using dbContext directly in the controller (even in the view template).
I am personally not a big fan of this approach. It creates a lot of noise in the controller as it knows your data persistence logic. And really, controller should not need to know how we persist the data, all it should worry about is orchestrating the view and let someone else to worry about handling business logic, data persistence, etc. 

If you disagree with me it is okay just make sure you use the same dbContext per http request and dispose it when an http request finishes. 

If you are still with me so far that's great let's dive into the code.
Let's take a look at **_Repository.cs_**.
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

Both methods will defer execution but the first virtual method returns an IQueryable type which means you can do Linq-to-Sql. So you can query on the returned IQueryable further, the final query then will be executed in the db. 
On the other hand, the second method returns an IEnumerable, you can only do Linq-to-Object, meaning when you apply further query on the returned IEnumerable, it actually goes to the db first, loads the result into memory then your second query will be executed in the memory.

I let them in the good hands and believe you know what suits your situation the best.

One another thing to note is the parameter 'inCludeProperties'. It is to deep load the relevant navigation properties. In your code you can do something like this:
    
    incld = "NavEntityName1" + "," + "NavEntityName2" + "NavEntityName3.NavEntityName4";

    var query = _unitOfWork.Repository<MyClassToDeepLoad>().GetDbQueryable(incld); 

Notice the 'NavEntityName3.NavEntityName4'. This is how you get down to the deeper level of navigation properties. 

And that is our generic repository.

Now let's take a look at **_UnitOfWork.cs_**.

    // Interface
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> Repository<T>() where T : class;
        void Save();
        void Dispose();
    }
                
Take note that the IUnitOfWork extends IDisposable. This is how the ioc container can dispose our unit of work class.

Nothing fancy here, constructor injection so dbContext to be injected by Unity.

    public UnitOfWork(IDbContext context)
    {
        _context = context;
    }

Repositories are stored in a hash table. We first check if the repository is in the hash table, if not, we create an instance using reflection,
and store this instance in the hash table by using its type name as key.   

    public IRepository<T> Repository<T>() where T : class
    {
        if (_repositories == null)
            _repositories = new Hashtable();

        var type = typeof(T).Name;

        if (!_repositories.ContainsKey(type))
        {
            var repositoryType = typeof(Repository<>);

            var repositoryInstance =
                Activator.CreateInstance(repositoryType
                         .MakeGenericType(typeof(T)), _context);

            _repositories.Add(type, repositoryInstance);
        }

        return (IRepository<T>)_repositories[type];
    }

Make sure you implement the Dispose method so Unity can call it when an http request ends.
	
	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				_context.Dispose();
			}
		}
		_disposed = true;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

In **_Bootstrapper.cs_**, you make sure the same dbContext is being used per http request by newing a PerRequestLifetimeManager when you do Unity register.

	public static void RegisterTypes(IUnityContainer container)
    {
        container.RegisterInstance(GlobalConfiguration.Configuration);

        container.RegisterType<IMyService, MyService>();   
        container.RegisterType<IUnitOfWork, UnitOfWork>(new PerRequestLifetimeManager());
        container.RegisterType<IDbContext, vDbContext>(new PerRequestLifetimeManager());
    }

Then last in **_Global.asax.cs_**, add this line below in Application_Start(). 

	Bootstrapper.Initialise();
	
Voilà! Now you have a generic db access using Entity Framework and Unity ioc to ensure your db context is 'request safe'. I have seen some people try to make dbContext thread safe (by the way [Entity Framework db context is intrinsically not thread safe](http://stackoverflow.com/a/11034535/2391304)), but as a new feature in C# we can now use async and await so you can have multiple threads within an http request.










