// Program tested on Windows 10 64x, Windows 7 32x.
// supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.7.2"
// Program return type is void
// Arguments could be given at launch from Windows Command Line or either after launch
// All arguments should be given at "one bunch".
// First two argument, separated with whitespace,
// are required and should point to two files.
// All other arguments are optional, one, two or three args could be given
// Optional arguments could be given in any order and combination, prefixed accordingly:
// for StartWith line number pointing "-s " or "--start "
// fo End-After line number pointing "-e " or "--end "
// for Excluded line numbers pointing "-x " or "--exclude ".
// Whitespace-separated integer values for "-x ".

//Issue list
// store some strings into static members
// make excluded collection elements unique (optional)
// (startwith, endwith, excluded) args cannot be greater than Int32.Max or less than 0
// not all encoding pages are handled
// "wild character" (*) handling not works correct at two files with different encoding

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace pcompare {
    class Input {
        bool result = false;
        static string hello = "Укажите пути к файлам 1 и 2";
        static string wrongpaths = "Указанные файлы не существуют";
        static string wrongfirst = "Файл 1 не существует";
        static string wrongsecond = "Файл 2 не существует";
        static string wrongstart = nl + "Номер строки начала сравнения указан некорректно";
        static string wrongend = nl + "Номер последней строки сравнения указан некорректно";
        static string lessthan = nl + "Номер последней строки не может быть меньше номера начальной";
        static string noexclude = nl + "Номера строк исключаемых из сравнения указаны некорректно";
        static string mistake_one = "Вы указали путь к файлу 1: ";

        static string nl = Environment.NewLine;
        string current_a = "";
        string current_b = "";
        string response = "";
        string[] args;
        int startwith = 0; // 0 for "compare from first"
        int endwith = -1; // -1 for "compare to the end"
        int[] excluded;

        // Constructor
        public Input(string[] a) {
            this.args =
                a != null ?
                    a.Length == 0 ? new string[] { "" } : a
                : ReadInput();
            if (!Check(args)) // is bad
                new Input(ReadInput());
            //else {
                //Write("Нажмите любую клавишу, чтобы выйти из приложения");
                //Console.ReadKey();
            //}

        }

        string[] ReadInput() {
            return Console.ReadLine().Replace("\"", "").Replace("'", "").Trim().Split(' ');
        }
        void Write(string m) {
            Console.WriteLine(m);
        }

        bool Check(string[] args) {
            int lenargs = args.Length;
            if (lenargs < 2) { // only one arg given
                if (lenargs == 0)
                    result = false;
                else if (lenargs == 1) {
                    if (args[0] == "")
                        result = false;
                    else {
                        response = mistake_one + args[0];// only one file path given
                        result = false;
                    }
                }
            }
            else if (lenargs > 1) // two or more args given
                ParseArgs();

            if (response != "") { Write(response); }
            if (!result) { Write(hello); }
            else
                PreCompareNotifier();
            return result;
        }

        /**
         * Tries to set all given agruments
         */
        void ParseArgs() {
            int pathscheck = ChekFilePaths(args[0], args[1]);
            response += pathscheck == -7 ? wrongpaths
                : pathscheck == -1 ? wrongfirst + " (" + args[0] + ")"
                : pathscheck == -2 ? wrongsecond + " (" + args[1] + ")" : "";
            result = pathscheck >= 0;

            for (int i = 2; i < args.Length; i++) { // more than 2 args, something beside paths given
                if (args[i] == "--start" || args[i] == "-s") { // Getting --start
                    if (i + 1 < args.Length && Int32.TryParse(args[i + 1], out startwith)) {
                        result = pathscheck >= 0 ? (startwith >= 0 ? true : false) : false;
                        if (endwith > -1 && endwith < startwith) { // if endwith is already given too, but is less than start
                            response += lessthan + " (" + startwith + " > " + endwith + ")";
                            result = false;
                        }
                    }
                    else { // Can't get --start
                        response += (i + 1 < args.Length) ? wrongstart + " (" + args[i + 1] + ")"
                            : wrongstart + " (" + args[i] + ")";
                        result = false;
                    }
                }

                if (args[i] == "--end" || args[i] == "-e") { // Getting --end
                    if (i + 1 < args.Length && Int32.TryParse(args[i + 1], out endwith)) {
                        result = pathscheck >= 0 ? ((endwith >= 0) ? true : false) : false;
                        if (startwith >= 0 && endwith > -1 && endwith < startwith) {// if startwith is already given too, but is greater than end
                            response += lessthan + " (" + endwith + " < " + startwith + ")";
                            result = false;
                        }
                    }
                    else { // Can't get --end
                        response += (i + 1 < args.Length) ? wrongend + " (" + args[i + 1] + ")"
                            : wrongend + " (" + args[i] + ")";
                        result = false;
                    }
                }

                if (args[i] == "--exclude" || args[i] == "-x") { // Getting --exclude from (i + 1) arg on
                    if (GetIntArray(i + 1))
                        result = true;
                    else { // Can't get --exclude
                        response += noexclude;
                        result = false;
                    }
                }
            }
        }

        /**
         * Prepares data for comparison
         */
        void PreCompareNotifier() {
            Write("Время начала сравнения (s:ms) " + DateTime.Now.Second.ToString() + ":" + DateTime.Now.Millisecond.ToString());
            if (args.Length > 1) {
                if (startwith > 0) { // startwith given
                    if (endwith >= 0) { // and endwith given
                        if (startwith == endwith && excluded == null)
                            Write("Сравнение ограничится строкой " + startwith);
                        else if (startwith != endwith && excluded == null) {
                            Write("Сравнение начнётся со строки " + startwith);
                            Write("Сравнение прекратится после строки " + endwith);
                        }
                        else { // start, end, x
                            Write("Сравнение начнётся со строки " + startwith);
                            Write("Сравнение прекратится после строки " + endwith);
                            Write("Следующие строки будут исключены из сравнения " + string.Join(", ", excluded));
                        }
                    }
                    else { // no -e
                        if (excluded == null) // no -x
                            Write("Сравнение начнётся со строки " + startwith);
                        else { // -s -x
                            Write("Сравнение начнётся со строки " + startwith);
                            Write("Следующие строки будут исключены из сравнения " + string.Join(", ", excluded));
                        }
                    }
                }
                else { // no -s
                    if (endwith > 0) { // -e
                        if (excluded == null) {// no -x
                            Write("Сравнение прекратится после строки " + endwith);
                        }
                        else { // -e -x
                            Write("Сравнение прекратится после строки " + endwith);
                            Write("Следующие строки будут исключены из сравнения " + string.Join(", ", excluded));
                        }
                    }
                    else if (excluded != null) // -x
                        Write("Следующие строки будут исключены из сравнения " + string.Join(", ", excluded));
                    else // no other things
                        Write("Сравниваются файлы... " + args[0] + " и " + args[1]);
                }
                TryCompareFiles(args[0], args[1], startwith);
            }
        }

        bool IsExcluded(int val) {
            bool has = false;
            if (excluded == null)
                return false;
            for (int i = 0; i < excluded.Length; i++) {
                if (excluded[i] == val) {
                    has = true;
                    break;
                }
            }
            return has;
        }

        /**
         * Sets values to int[] excluded and Returns true if values are set.
         * Else Returns false.
         */
        bool GetIntArray(int from) {
            int check;
            if (from > args.Length - 1 || !Int32.TryParse(args[from], out check)) { // if out of range OR is NaN
                excluded = null;
                return false;
            }
            int a = from;
            try {
                while (a < args.Length && !args[a].StartsWith("-") && (Int32.Parse(args[a]) >= startwith)) {
                    if ((endwith == -1) || endwith != -1 && !(Int32.Parse(args[a]) > endwith)) // not given -end || given -end && x !> E
                        a++;
                }
                if (a - from == 0)
                    return false;

                excluded = new int[a - from];
                for (int i = 0; i < excluded.Length; i++) {
                    if (!(Int32.Parse(args[i + from]) < startwith)) { // if not OoBounds
                        if ((endwith == -1) || endwith != -1 && !(Int32.Parse(args[i + from]) > endwith)) // not given -end || given -end && x !> -end
                            excluded[i] = Int32.Parse(args[i + from]);
                    }
                }
                return true;
            }
            catch {
                excluded = null;
                return false;
            }
        }

        /**
         * all wrong -> -7, wrong b -> -2, wrong a -> -1, all correct -> 0
         */
        int ChekFilePaths(string a, string b) {
            bool bad_a = !File.Exists(a);
            bool bad_b = !File.Exists(a);
            return (bad_a && bad_b) ? -7 : bad_a ? -1 : bad_b ? -2 : 0;
        }

        void TryCompareFiles(string a, string b, int offset = 0) {
            try {
                using (var r_a = new StreamReader(a, true)) {
                    r_a.Read();
                }
            }
            catch {
                Write("Ошибка чтения файла " + a);
                return;
            }
            try {
                using (var r_b = new StreamReader(b, true)) {
                    r_b.Read();
                }
            }
            catch {
                Write("Ошибка чтения файла " + b);
                return;
            }
            Encoding enc = Encoding.Default;
            int ai = 0;
            /* Читаем оба файла "нормально", пока не наткнёмся на 1-ое различие */
            using (var reader_a = new StreamReader(a, true)) {
                using (var reader_b = new StreamReader(b, true)) {
                    while (current_a == current_b && !reader_a.EndOfStream) {
                        if (ai > endwith - 1 && endwith > 0)
                            break;
                        if (reader_b.EndOfStream) {
                            current_a = reader_a.ReadLine(); // Читаем + Пишем А
                            current_b = reader_b.ReadLine(); // null
                            Write(string.Format("Строки номер ({0}) не существует в файле {1}", ai, b));
                            break;
                        }
                        while (ai <= offset || IsExcluded(ai + 1)) {
                            reader_a.ReadLine(); // Skip theese lines for both files
                            reader_b.ReadLine();
                            ai++;
                        }
                        current_a = reader_a.ReadLine(); // Читаем + Пишем А
                        current_b = reader_b.ReadLine(); // Читаем + Пишем Б
                        ai++;
                    } // Возможно строки отличаются
                } // Закрываем поток Б
            } // Закрываем поток А

            if (current_a != null && current_b != null && current_a != current_b) {
                int bad_chars_count_a = new List<char>(current_a).FindAll(x => x == '�').Count;
                int bad_chars_count_b = new List<char>(current_b).FindAll(x => x == '�').Count;

                if (bad_chars_count_a > bad_chars_count_b) { // Если в А больше "вопросов", чем в Б
                    int i = 0;
                    using (var reader_a_x = new StreamReader(a, enc, true)) { // Читаем А по-другому,
                        while (i < ai - 1 || IsExcluded(ai + 1)) { // Skip theese lines
                            reader_a_x.ReadLine();
                            i++;
                        }
                        current_a = reader_a_x.ReadLine();
                    }
                    if (new List<char>(current_a).FindAll(x => x == '�').Count < bad_chars_count_a) // Вопросов стало меньше
                        TryCompareWithNewEncoding(a, b, ai, 1); // Продолжить сравнение по новой схеме (А_x, Б)
                }
                else if (bad_chars_count_b > bad_chars_count_a) { // Или Если в Б больше "вопросов", чем в А
                    int i = 0;
                    using (var reader_b_x = new StreamReader(b, enc, true)) { // Б -- по-другому
                        while (i < ai - 1) {
                            reader_b_x.ReadLine();
                            i++;
                        }
                        current_b = reader_b_x.ReadLine();
                    }
                    if (new List<char>(current_b).FindAll(x => x == '�').Count < bad_chars_count_b) // Вопросов стало меньше
                        TryCompareWithNewEncoding(a, b, ai, 2); // Продолжить сравнение по новой схеме (А, Б_x)
                }
                else { // Если "плохих символов" поровну
                    if (current_a.Contains("*")) { // И в строке есть "*", проверить на Звёздочки
                        bool became_equal = StarsMakesLinesEqual();
                        if (!became_equal) // И Если и Звёздочки не делают строки равными -- всё же строки разные
                            Write(string.Format("Различие найдено в строке номер ({0}){1}{2}{1}{3}", ai, nl, current_a, current_b));
                        else
                            Write((endwith != -1 || startwith > 0 || excluded != null) ? "Указанные строки в данных файлах равны"
                                : "Все строки равны в файлах " + a + " и " + b);
                    }
                    else { // В строке нет "*"
                        Write(string.Format("Различие найдено в строке номер ({0}){1}{2}{1}{3}", ai, nl, current_a, current_b));
                    }
                }
            }
            else { // current_a == current_b
                Write((endwith != -1 || startwith > 0 || excluded != null) ? "Указанные строки в данных файлах равны"
                    : "Все строки равны в файлах " + a + " и " + b);
            }
            Write("Время окончания сравнения (s:ms) " + DateTime.Now.Second.ToString() + ":" + DateTime.Now.Millisecond.ToString());
        }

        /**
         * Пытается сделать строки более читаемыми -- пробуя изменить кодировку
         */
        void TryCompareWithNewEncoding(string a, string b, int offset, int which_file_reencode) {
            Encoding enc = Encoding.Default;
            int ai = 0;
            using (var reader_a = which_file_reencode == 1 ? new StreamReader(a, enc, true) : new StreamReader(a, true)) {
                using (var reader_b = which_file_reencode == 1 ? new StreamReader(b, true) : new StreamReader(b, enc, true)) {
                    while (current_a == current_b && !reader_a.EndOfStream) {
                        if (ai > endwith - 1 && endwith > -1) // Дальше не надо
                            break;
                        if (reader_b.EndOfStream) {
                            current_a = reader_a.ReadLine(); // Читаем + Пишем А
                            current_b = reader_b.ReadLine(); // null
                            Write(string.Format("Строки номер ({0}) не существует в файле {1}", ai, b));
                            break;
                        }
                        while (ai < offset - 1 || IsExcluded(ai + 1)) { // no "-1" for the 1-st time! shift from 0 line to offset!
                            reader_a.ReadLine(); // Skip theese lines for both files
                            reader_b.ReadLine();
                            ai++;
                        }
                        current_a = reader_a.ReadLine(); // Читаем + Пишем А
                        current_b = reader_b.ReadLine(); // Читаем + Пишем Б
                        ai++;
                    } // Возможно строки отличаются
                } // Закрываем поток Б
            } // Закрываем поток А
            if (current_a == current_b) // Достигнута граница сравнения
                Write((endwith != -1 || startwith > 0 || excluded != null) ? "Указанные строки в данных файлах равны"
                    : "Все строки равны в файлах " + a + "и " + b);
            else if (current_a.Contains("*")) // Если в строке есть "*", проверить на Звёздочки
                if (StarsMakesLinesEqual() == false) // И Если и Звёздочки не делают строки равными -- всё же строки разные
                    Write(string.Format("Различие найдено в строке номер ({0}){1}{2}{1}{3}", ai, nl, current_a, current_b));
                else
                    Write((endwith != -1 || startwith > 0 || excluded != null) ? "Указанные строки в данных файлах равны"
                        : "Все строки равны в файлах " + a + "и " + b);
        }

        bool StarsMakesLinesEqual() { // checks just given current_a-to-current_b, not iterating over file lines.
            bool success = true; // does stars make strings equal

            List<string> current_a_parts = new List<string>(current_a.Split('*')); /* Method called if current_a.Contains("*") */
            current_a_parts.RemoveAll(part => part == "");
            if (current_a_parts.Count == 0) // all parts were removed as all-stars => current_a == "***" == current_b
                return success; // Обратно в цикл, запросивший *-check

            // Искать под-строки из current_a_parts в строке current_b
            int start = 0;
            for (int part = 0; part < current_a_parts.Count; part++) {
                int found_index = current_b.IndexOf(current_a_parts[part], start); // by now current_b definitely contains Part

                start = found_index + current_a_parts[part].Length; // in described at comments case, Length is always "1"
                if (start > current_b.Length /* out of bounds because of wrong order parts in current_b */
                    || found_index < 0 /* not found */) {
                    success = false;
                    break;
                }
            }
            return success; // true at init
        }
    }

    class Program {
        static void Main(string[] args) {
            new Input(args);
        }
    }
}
