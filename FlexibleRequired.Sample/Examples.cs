// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using System;

namespace FlexibleRequired.Sample;

public class E {

    public static void Main() {
        A a = new A()
        {
            b = 10,
            a = 0,
            c = 0
        };
        B b = new B()
        {
            b = 0,
            c = 0,
            a = 0
        };
    }

}

public class A {

    [Required]
    public virtual int a { get; init; }

    public virtual int b { get; init; }

    [Required]
    public virtual int c { get; init; }

}

public class B : A {

    [Required(false)]
    public override int a { get; init; }

    [Required]
    public override int b { get; init; }

}

