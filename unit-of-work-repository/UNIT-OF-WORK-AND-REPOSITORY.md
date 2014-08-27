Generic Unit of Work with Entity Framework and Unity
===========

In this article I will to explain how to build a generic abstraction layer that sits between business logic layer and data access layer with Entity Framework and Microsoft Unity.

Here is an illustration of our layered architecture.

![layered mvc architeture](https://github.com/Konex/asp.net-mvc/blob/master/unit-of-work-repository/images/layered-app-mvc.png)

Before we dive in, let's step back and ask why all the hassle, why can't we just use DbContext in controller? Well, technically speaking, yes we can. And in the past I have seen people use DbContext directly in controllers, even in the view templates in real-life projects.

I am personally not a big fan of this approach. First of all, straight away I can see it creates noise in controller by decorating 'using' code blocks to seal DbContext taking the advantage that DbContext is disposed automatically. But with using statement, you can't catch exceptions. So you fall back to disposing DbContext manually.

	try
	{

	}
	catch(Exception ex)
	{

	}
	finally
	{
		_dbContext.Dispose();
	}

Apart from the noise, controller now knows your data persistence logic and is responsible for disposing DbContext. That doesn't look good to me because now your view logic and data persistence logic are meshed up together. This will make unit testing or change data store a pain.
 
Furthermore, if your app is in such a scale that you need to deploy it into a server farm where you have a web server hosting your web project, and an application server for EF data persistence project. As you use DbContext directly in the controller, then your EF data persistence is running in different processes. EF is intrinsically not thread safe, you will have to write a great deal of code to make it work in scenario like this. Nevertheless, putting all EF related stuff in one place in general is a good practice doesn't matter how big your app is. 

That's enough justification so let's see how we can create a generic layer with Unit of work and Repository patterns. I have read quite a few articles on the Net about how to implement Unit of Work and Repository patterns in an ASP.NET app with Entity Framework. But quite often the scenarios and samples in those articles are too simple and hardly any one of them gives me a thorough walk-through on how do make Unit of Work and Repository classes generic. [This article](http://www.asp.net/mvc/tutorials/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application) from ASP.NET MVC site however gives me some hints on how to go about implementing one.

Let's take a look at **_Repository.cs_**. 
It is a generic repository and everything is pretty self-explanatory only two methods here deserve a bit of attention.
    
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

Both methods will defer execution but the first virtual method returns an IQueryable type which means you can do Linq-to-Sql. So you can query on the returned IQueryable further, the final query then will be executed in the database. 
On the other hand, the second method returns an IEnumerable, you can only do Linq-to-Object, meaning when you apply further query on the returned IEnumerable, it actually goes to the database first, loads result into memory then your second query will be executed in memory.

I provide both methods here and believe you know what suits your situation the best.

One another thing to note is the 'inCludeProperties' parameter. It is to deep load the relevant navigation properties. In your business logic layer you can do something like this:
    
    incld = "NavEntityName1" + "," + "NavEntityName2" + "," + "NavEntityName3.NavEntityName4";

    var query = _unitOfWork.Repository<MyClassToDeepLoad>().GetDbQueryable(incld); 

Notice the 'NavEntityName3.NavEntityName4'. This is how you get down to the deeper levels of object graph. And that is our generic repository class.

Now let's take a look at **_UnitOfWork.cs_**.

    // Interface
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> Repository<T>() where T : class;
        void Save();
        void Dispose();
    }
                
Take note that the IUnitOfWork extends IDisposable. This is how the ioc container can dispose our unit of work class.

Nothing fancy here, constructor injection so DbContext to be injected by Unity.

    public UnitOfWork(IDbContext context)
    {
        _context = context;
    }

Repositories are stored in a dictionary. We first check if the repository is in the dictionary, if not, we create an instance using reflection,
and store this instance in the dictionary by using its type name as key.   

    public IRepository<T> Repository<T>() where T : class
    {
        if (_repositories == null)
            _repositories = new Dictionary<string, object>();

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
	
	// Technically, we don't need an overloaded version of Dispose in here unless
	// your unit of work class accesses resources through unmanaged code, then
	// you would need to implement a Finalize method (commented out below).
	//
	// But here I just want to demonstrate in a more complicated scenario that your 
	// clean up code might need to access external objects then you should only do 
	// so during disposing.
	protected virtual void Dispose(bool disposing)
	{
		if (_disposed) return;
		
		if (disposing)
		{	
			// This unit of work object is being disposed but not finalized yet.
			// So it is still safe to access other objects (except the base object)
			// only from inside this code block.
			_context.Dispose();
		}

		// Perform cleanup tasks here that need to be done in either Dispose and Finalize
		// ...	
	}

	// ~UnitOfWork()
	// {
	// 	Dispose(false);
	// }
	
	public void Dispose()
	{
		Dispose(true);
		_disposed = true;
		// Because calling Finalize method has a performance hit,
		// so we tell .NET not to call the Finalize method
		// after we have disposed the DbContext.
		GC.SuppressFinalize(this);
	}
	
In **_Bootstrapper.cs_**, you make sure the same DbContext is being used per http request by newing a PerRequestLifetimeManager when you do Unity register.

	public static void RegisterTypes(IUnityContainer container)
    {
        container.RegisterInstance(GlobalConfiguration.Configuration);

        container.RegisterType<IMyService, MyService>();   
        container.RegisterType<IUnitOfWork, UnitOfWork>(new PerRequestLifetimeManager());
        container.RegisterType<IDbContext, vDbContext>(new PerRequestLifetimeManager());
    }

Then last in **_Global.asax.cs_**, add this line below in Application_Start(). 

	Bootstrapper.Initialise();
	
Voilà! Now you have a generic abstraction layer with Entity Framework and Unity to ensure your db context is 'request safe'. (by the way [Entity Framework DbContext is intrinsically not thread safe](http://stackoverflow.com/a/11034535/2391304)) Since ASP.NET is using thread pool, also as a new feature in C# we can now use async and await so you can even have multiple threads within an http request. So you need to be extra careful about multiple DbContext instances might get created per thread as a single web request can spawn multiple threads, and ASP.NET thread pooling meaning DbContext instance might get cached up and lives as long as the host thread lives. This is a bad bad bad situation and your users might see inconsistent data. Well, I think I better stop here as DbContext thread safe deserves a full on [discussion](http://stackoverflow.com/a/3266481/2391304) on its own. 

I hope you enjoy reading this article. If you see anything you can improve please let me know.

Merci beaucoup!   










