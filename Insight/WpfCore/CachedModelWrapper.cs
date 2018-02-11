using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Insight.WpfCore
{
    /// <summary>
    /// Wraps a model element
    /// </summary>
    public class CachedModelWrapper<T> : NotifyDataErrorInfoBase
    {
        private readonly Dictionary<PropertyInfo, object> _modifications = new Dictionary<PropertyInfo, object>();

        public CachedModelWrapper(T model)
        {
            Model = model;
        }

        public T Model { get; }

        public void Apply()
        {
            foreach (var modification in _modifications)
            {
                var propertyInfo = modification.Key;
                propertyInfo.SetValue(Model, modification.Value);
            }
        }

        protected void ClearModifications()
        {
            _modifications.Clear();
        }


        protected virtual TValue GetValue<TValue>([CallerMemberName] string propertyName = null)
        {
            return (TValue) GetValueObject(propertyName);
        }

        protected object GetValueObject([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var propertyInfo = typeof(T).GetProperty(propertyName);
            if (propertyInfo == null)
            {
                throw new InvalidOperationException("Property " + nameof(propertyName) + " not found on model element!");
            }

            // If we have a modification, take it instead of the original value
            if (_modifications.TryGetValue(propertyInfo, out var modifiedValue))
            {
                return modifiedValue;
            }

            return propertyInfo.GetValue(Model);
        }

        protected virtual void SetValue<TValue>(TValue value, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var propertyInfo = typeof(T).GetProperty(propertyName);
            if (propertyInfo == null)
            {
                throw new InvalidOperationException("Property " + nameof(propertyName) + " not found on model element!");
            }

            if (_modifications.ContainsKey(propertyInfo))
            {
                _modifications[propertyInfo] = value;
            }
            else
            {
                _modifications.Add(propertyInfo, value);
            }

            //propertyInfo.SetValue(Model, value);
            OnPropertyChanged(propertyName);
            ValidatePropertyInternal(propertyName, value);
        }

        protected void ValidateNow(string propertyName)
        {
            ValidatePropertyInternal(propertyName, GetValueObject(propertyName));
        }

        protected virtual IEnumerable<string> ValidateProperty(string propertyName)
        {
            return null;
        }

        private void ValidateCustomErrors(string propertyName)
        {
            var erros = ValidateProperty(propertyName);

            if (erros != null)
            {
                foreach (var error in erros)
                {
                    AddError(propertyName, error);
                }
            }
        }

        private void ValidateDataAnnotations(string propertyName, object currentValue)
        {
            var context = new ValidationContext(Model) { MemberName = propertyName };
            var results = new List<ValidationResult>();
            Validator.TryValidateProperty(currentValue, context, results);
            foreach (var result in results)
            {
                AddError(propertyName, result.ErrorMessage); // TODO transation.
            }
        }

        private void ValidatePropertyInternal(string propertyName, object currentValue)
        {
            ClearErrors(propertyName);
            ValidateDataAnnotations(propertyName, currentValue);
            ValidateCustomErrors(propertyName);
        }
    }
}