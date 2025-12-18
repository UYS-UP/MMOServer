using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Server.Utility
{
    public static class Validator
    {
        /// <summary>
        /// 验证密码是否符合要求
        /// </summary>
        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // 密码规则：至少8位，只包含字母和数字
            string pattern = @"^[a-zA-Z0-9]{8,}$";
            return Regex.IsMatch(password, pattern);
        }

        /// <summary>
        /// 验证账号是否是邮箱格式
        /// </summary>
        public static bool IsValidEmail(string account)
        {
            if (string.IsNullOrWhiteSpace(account))
                return false;

            // 简单的邮箱正则表达式
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(account, pattern, RegexOptions.IgnoreCase);
        }
    }
}
