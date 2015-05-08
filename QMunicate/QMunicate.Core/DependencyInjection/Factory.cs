﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace QMunicate.Core.DependencyInjection
{
    public class Factory
    {
        private Dictionary<Type, TypeBindingBase> typeBindings = new Dictionary<Type, TypeBindingBase>();

        static Factory()
        {
            CommonFactory = new Factory();
        }

        public static Factory CommonFactory { get; private set; }

        public void Initialize(FactoryInitializer initializer)
        {
            initializer.SetBindings(this);
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Type parameter passed to TypeBinding class.")]
        public void Bind<TSource, TReal>(LifetimeMode mode) where TReal : TSource, new()
        {
            var binding = new TypeBinding<TSource, TReal>();
            binding.Mode = mode;
            var sourceType = typeof(TSource);
            this.typeBindings[sourceType] = binding;
        }

        public TSource GetInstance<TSource>()
        {
            TypeBindingBase binding = null;
            var sourceType = typeof(TSource);
            bool boundExist = this.typeBindings.TryGetValue(sourceType, out binding);
            if (!boundExist)
            {
                var errorMessage = String.Format(CultureInfo.InvariantCulture, "Type {0} was not bound to any real type. Call Bind method for this type.", sourceType.FullName);
                throw new InvalidOperationException(errorMessage);
            }

            var instance = binding.GetRealInstance();
            return (TSource)instance;
        }

        public bool IsBindingExist(Type checkedType)
        {
            var isExist = this.typeBindings.ContainsKey(checkedType);
            return isExist;
        }
    }
}
