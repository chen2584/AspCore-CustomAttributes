# AspCore-CustomAttributes

### OutputCache Attribute
Are you missing OutputCache attribute on ASP.NET MVC?, Here is my own OutputCache attribute for ASP.NET Core

### Basic Usage
```
[OutputCache(Duration=30)] //where 30 = seconds before cache expire.
public ActionResult Index()
{
    return View();
}
```

Hope you like it!
