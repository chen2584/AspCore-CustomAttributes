# AspCore-CustomAttributes

### OutputCache Attribute
Are you missing OutputCache attribute on ASP.NET MVC?, Here is my own OutputCache attribute for ASP.NET Core

### Basic Usage
```
[OutputCache(Duration=30)] //where 30 = seconds before cache expire.
public ActionResult Index()
{
    var result = new { FirstName = "Chen", LastName = "Angelo" };
    Console.WriteLine("In Action Main");
    return Ok(result);
}
```

Hope you like it!
