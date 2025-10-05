using System;
using Microsoft.AspNetCore.Mvc;

namespace ApiEcommerce.Constants;

public class CacheProfiles
{
  public const string Cache1 = "Default10";
  public const string Cache2 = "Default20";


  public static readonly CacheProfile Profile1 = new()
  {
    Duration = 10,
  };
  public static readonly CacheProfile Profile2 = new()
  {
    Duration = 20,
  };
}
