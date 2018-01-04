
using System;
using System.Collections.Generic;

namespace FuyukaiHWMonitor.Collections {

  public interface IReadOnlyArray<T> : IEnumerable<T> {

    T this[int index] { get; }

    int Length { get; }
    
  }
}
