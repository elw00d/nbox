﻿using System;
using System.Collections.Generic;

namespace NBox.Utils
{
    /// <summary>
    /// Indicates that the value of marked element could be <c>null</c> sometimes, so the check for <c>null</c> is necessary before its usage
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Delegate | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class CanBeNullAttribute : Attribute
    {
    }

    /// <summary>
    /// Indicates that the function argument should be string literal and match one  of the parameters of the caller function.
    /// For example, <see cref="ArgumentNullException"/> has such parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class InvokerParameterNameAttribute : Attribute
    {
    }

#if !LOADER

    /// <summary>
    /// Indicates that marked method builds string by format pattern and (optional) arguments. 
    /// Parameter, which contains format string, should be given in constructor.
    /// The format string should be in <see cref="string.Format(IFormatProvider,string,object[])"/> -like form
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class StringFormatMethodAttribute : Attribute
    {
        private readonly string myFormatParameterName;

        /// <summary>
        /// Initializes new instance of StringFormatMethodAttribute
        /// </summary>
        /// <param name="formatParameterName">Specifies which parameter of an annotated method should be treated as format-string</param>
        public StringFormatMethodAttribute(string formatParameterName) {
            myFormatParameterName = formatParameterName;
        }

        /// <summary>
        /// Gets format parameter name
        /// </summary>
        public string FormatParameterName {
            get {
                return myFormatParameterName;
            }
        }
    }

    /// <summary>
    /// Indicates that the marked method is assertion method, i.e. it halts control flow if one of the conditions is satisfied. 
    /// To set the condition, mark one of the parameters with <see cref="AssertionConditionAttribute"/> attribute
    /// </summary>
    /// <seealso cref="AssertionConditionAttribute"/>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class AssertionMethodAttribute : Attribute
    {
    }

    /// <summary>
    /// Indicates the condition parameter of the assertion method. 
    /// The method itself should be marked by <see cref="AssertionMethodAttribute"/> attribute.
    /// The mandatory argument of the attribute is the assertion type.
    /// </summary>
    /// <seealso cref="AssertionConditionType"/>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class AssertionConditionAttribute : Attribute
    {
        private readonly AssertionConditionType myConditionType;

        /// <summary>
        /// Initializes new instance of AssertionConditionAttribute
        /// </summary>
        /// <param name="conditionType">Specifies condition type</param>
        public AssertionConditionAttribute(AssertionConditionType conditionType) {
            myConditionType = conditionType;
        }

        /// <summary>
        /// Gets condition type
        /// </summary>
        public AssertionConditionType ConditionType {
            get {
                return myConditionType;
            }
        }
    }

    /// <summary>
    /// Specifies assertion type. If the assertion method argument satisifes the condition, then the execution continues. 
    /// Otherwise, execution is assumed to be halted
    /// </summary>
    public enum AssertionConditionType
    {
        /// <summary>
        /// Indicates that the marked parameter should be evaluated to true
        /// </summary>
        IsTrue = 0,

        /// <summary>
        /// Indicates that the marked parameter should be evaluated to false
        /// </summary>
        IsFalse = 1,

        /// <summary>
        /// Indicates that the marked parameter should be evaluated to null value
        /// </summary>
        IsNull = 2,

        /// <summary>
        /// Indicates that the marked parameter should be evaluated to not null value
        /// </summary>
        IsNotNull = 3,
    }

    /// <summary>
    /// Indicates that the marked method unconditionally terminates control flow execution.
    /// For example, it could unconditionally throw exception
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class TerminatesProgramAttribute : Attribute
    {
    }

    

    /// <summary>
    /// Indicates that the value of marked element could never be <c>null</c>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Delegate | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class NotNullAttribute : Attribute
    {
    }

    /// <summary>
    /// Indicates that the value of marked type (or its derivatives) cannot be compared using '==' or '!=' operators.
    /// There is only exception to compare with <c>null</c>, it is permitted
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class CannotApplyEqualityOperatorAttribute : Attribute
    {
    }

    /// <summary>
    /// When applied to target attribute, specifies a requirement for any type which is marked with 
    /// target attribute to implement or inherit specific type or types
    /// </summary>
    /// <example>
    /// <code>
    /// [BaseTypeRequired(typeof(IComponent)] // Specify requirement
    /// public class ComponentAttribute : Attribute 
    /// {}
    /// 
    /// [Component] // ComponentAttribute requires implementing IComponent interface
    /// public class MyComponent : IComponent
    /// {}
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    [BaseTypeRequired(new Type [] { typeof(Attribute) })]
    public sealed class BaseTypeRequiredAttribute : Attribute
    {
        private readonly Type[] myBaseTypes;

        /// <summary>
        /// Initializes new instance of BaseTypeRequiredAttribute
        /// </summary>
        /// <param name="baseTypes">Specifies which types are required</param>
        public BaseTypeRequiredAttribute(params Type[] baseTypes) {
            myBaseTypes = baseTypes;
        }

        /// <summary>
        /// Initializes new instance of BaseTypeRequiredAttribute
        /// </summary>
        /// <param name="baseTypes">Specifies types are required</param>
        public BaseTypeRequiredAttribute(IEnumerable<Type> baseTypes) {
            myBaseTypes = new List<Type>(baseTypes).ToArray();
        }


        /// <summary>
        /// Gets enumerations of specified base types
        /// </summary>
        public IEnumerable<Type> BaseTypes {
            get {
                return myBaseTypes;
            }
        }
    }

    /// <summary>
    /// Indicates that the value of marked element should be initialized from IoC/DI container like Spring .NET.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event)]
    public sealed class SpringInitializedAttribute : Attribute
    {
    }
#endif
}