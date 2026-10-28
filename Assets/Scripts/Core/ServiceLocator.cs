using System;
using System.Collections.Generic;

namespace DesktopDoomsday.Core
{
    /// <summary>
    /// 轻量服务定位器。启动时注册单例（GameState、SaveSystem、各 System），运行期只读获取。
    /// 禁止在热路径中用 Find/GetComponent，统一走这里。
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            _services[typeof(T)] = service;
        }

        public static T Get<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var s) ? (T)s : null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s))
            {
                service = (T)s;
                return true;
            }
            service = null;
            return false;
        }
    }
}
