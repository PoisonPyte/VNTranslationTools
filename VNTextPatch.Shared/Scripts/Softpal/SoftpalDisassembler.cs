﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VNTextPatch.Shared.Util;

namespace VNTextPatch.Shared.Scripts.Softpal
{
    public class SoftpalDisassembler
    {
        private static readonly Dictionary<short, (string, string)> Opcodes =
            new Dictionary<short, (string, string)>
            {
                { 0x0001, ("mov", "pp") },
                { 0x0002, ("add", "pp") },
                { 0x0003, ("sub", "pp") },
                { 0x0004, ("mul", "pp") },
                { 0x0005, ("div", "pp") },
                { 0x0006, ("binand", "pp") },
                { 0x0007, ("binor", "pp") },
                { 0x0008, ("binxor", "pp") },
                { 0x0009, ("jmp", "p") },           // p = index of label in point.dat
                { 0x000A, ("jz", "ip") },           // label index, condition value
                { 0x000B, ("call", "p") },
                { 0x000C, ("eq", "pp") },
                { 0x000D, ("neq", "pp") },
                { 0x000E, ("le", "pp") },
                { 0x000F, ("ge", "pp") },
                { 0x0010, ("lt", "pp") },
                { 0x0011, ("gt", "pp") },
                { 0x0012, ("logor", "pp") },
                { 0x0013, ("logand", "pp") },
                { 0x0014, ("not", "i") },
                { 0x0015, ("exit", "") },
                { 0x0016, ("nop", "") },
                { 0x0017, ("syscall", "ii") },
                { 0x0018, ("ret", "") },
                { 0x0019, (null, "") },
                { 0x001A, ("mod", "pp") },
                { 0x001B, ("shl", "pp") },
                { 0x001C, ("sar", "pp") },
                { 0x001D, ("neg", "i") },
                { 0x001E, ("pop", "p") },
                { 0x001F, ("push", "p") },
                { 0x0020, ("enter", "p") },
                { 0x0021, ("leave", "p") },
                { 0x0023, ("create_message", "") },
                { 0x0024, ("get_message", "") },
                { 0x0025, ("get_message_param", "") },
                { 0x0028, ("se_load", "") },
                { 0x0029, ("se_play", "") },
                { 0x002A, ("se_play_ex", "") },
                { 0x002B, ("se_stop", "") },
                { 0x002C, ("se_set_volume", "") },
                { 0x002D, ("se_get_volume", "") },
                { 0x002E, ("se_unload", "") },
                { 0x002F, ("se_wait", "") },
                { 0x0030, ("set_se_info", "") },
                { 0x0031, ("get_se_ex_volume", "") },
                { 0x0032, ("set_se_ex_volume", "") },
                { 0x0033, ("se_enable", "") },
                { 0x0034, ("is_se_enable", "") },
                { 0x0035, ("se_set_pan", "") },
                { 0x0036, ("se_mute", "") },
                { 0x0038, ("select_init", "") },
                { 0x0039, ("select", "") },
                { 0x003A, ("select_add_choice", "") },
                { 0x003B, ("end_select", "") },
                { 0x003C, ("select_clear", "") },
                { 0x003D, ("select_set_offset", "") },
                { 0x003E, ("select_set_process", "") },
                { 0x003F, ("select_lock", "") },
                { 0x0040, ("get_select_on_key", "") },
                { 0x0041, ("get_select_pull_key", "") },
                { 0x0042, ("get_select_push_key", "") },
                { 0x0044, ("skip_set", "") },
                { 0x0045, ("skip_is", "") },
                { 0x0046, ("auto_set", "") },
                { 0x0047, ("auto_is", "") },
                { 0x0048, ("auto_set_time", "") },
                { 0x0049, ("auto_get_time", "") },
                { 0x004A, ("window_set_mode", "") },
                { 0x004B, (null, "") },
                { 0x004C, (null, "") },
                { 0x004D, (null, "") },
                { 0x004E, (null, "") },
                { 0x004F, ("effect_enable_is", "") },
                { 0x0050, ("cursor_pos_get", "") },
                { 0x0051, ("time_get", "") },
                { 0x0052, (null, "") },
                { 0x0053, ("load_font", "") },
                { 0x0054, ("unload_font", "") },
                { 0x0055, ("set_font_type", "") },
                { 0x0056, ("key_cancel", "") },
                { 0x0057, ("set_font_color", "") },
                { 0x0058, ("load_font_ex", "") },
                { 0x0059, (null, "") },
                { 0x005A, (null, "") },
                { 0x005B, ("lpush", "") },                  // push label onto callstack
                { 0x005C, ("lpop", "") },
                { 0x005D, (null, "") },
                { 0x005E, (null, "") },
                { 0x005F, ("set_font_size", "") },
                { 0x0060, ("get_font_size", "") },
                { 0x0061, ("get_font_type", "") },
                { 0x0062, ("set_font_effect", "") },
                { 0x0063, ("get_font_effect", "") },
                { 0x0064, ("get_pull_key", "") },
                { 0x0065, ("get_on_key", "") },
                { 0x0066, ("get_push_key", "") },
                { 0x0067, ("input_clear", "") },
                { 0x0068, ("change_window_size", "") },
                { 0x0069, ("change_aspect_mode", "") },
                { 0x006A, ("aspect_position_enable", "") },
                { 0x006B, (null, "") },
                { 0x006C, ("get_aspect_mode", "") },
                { 0x006D, ("get_monitor_size", "") },
                { 0x006E, ("get_window_pos", "") },
                { 0x006F, ("get_system_metrics", "") },
                { 0x0070, ("set_system_path", "") },
                { 0x0071, ("set_allmosaicthumbnail", "") },
                { 0x0072, ("enable_window_change", "") },
                { 0x0073, ("is_enable_window_change", "") },
                { 0x0074, ("set_cursor", "") },
                { 0x0075, ("set_hide_cursor_time", "") },
                { 0x0076, ("get_hide_cursor_time", "") },
                { 0x0077, ("scene_skip", "") },
                { 0x0078, ("cancel_scene_skip", "") },
                { 0x0079, ("lsize", "") },                  // get callstack size
                { 0x007A, ("get_async_key", "") },
                { 0x007B, ("get_font_color", "") },
                { 0x007C, ("get_current_date", "") },
                { 0x007D, ("history_skip", "") },
                { 0x007E, ("cancel_history_skip", "") },
                { 0x007F, (null, "") },
                { 0x0081, ("system_btn_set", "") },
                { 0x0082, ("system_btn_release", "") },
                { 0x0083, ("system_btn_enable", "") },
                { 0x0086, ("text_init", "") },
                { 0x0087, ("text_set_icon", "") },
                { 0x0088, ("text", "") },
                { 0x0089, ("text_hide", "") },
                { 0x008A, ("text_show", "") },
                { 0x008B, ("text_set_btn", "") },
                { 0x008C, ("text_uninit", "") },
                { 0x008D, ("text_set_rect", "") },
                { 0x008E, ("text_clear", "") },
                { 0x008F, (null, "") },
                { 0x0090, ("text_get_time", "") },
                { 0x0091, ("text_window_set_alpha", "") },
                { 0x0092, ("text_voice_play", "") },
                { 0x0093, (null, "") },
                { 0x0094, ("text_set_icon_animation_time", "") },
                { 0x0095, ("text_w", "") },
                { 0x0096, ("text_a", "") },
                { 0x0097, ("text_wa", "") },
                { 0x0098, ("text_n", "") },
                { 0x0099, ("text_cat", "") },
                { 0x009A, ("set_history", "") },
                { 0x009B, ("is_text_visible", "") },
                { 0x009C, ("text_set_base", "") },
                { 0x009D, ("enable_voice_cut", "") },
                { 0x009E, ("is_voice_cut", "") },
                { 0x009F, (null, "") },
                { 0x00A0, (null, "") },
                { 0x00A1, (null, "") },
                { 0x00A2, ("text_set_color", "") },
                { 0x00A3, ("text_redraw", "") },
                { 0x00A4, ("set_text_mode", "") },
                { 0x00A5, ("text_init_visualnovelmode", "") },
                { 0x00A6, ("text_set_icon_mode", "") },
                { 0x00A7, ("text_vn_br", "") },
                { 0x00A8, (null, "") },
                { 0x00A9, (null, "") },
                { 0x00AA, (null, "") },
                { 0x00AB, (null, "") },
                { 0x00AC, ("tips_get_str", "") },
                { 0x00AD, ("tips_get_param", "") },
                { 0x00AE, ("tips_reset", "") },
                { 0x00AF, ("tips_search", "") },
                { 0x00B0, ("tips_set_color", "") },
                { 0x00B1, ("tips_stop", "") },
                { 0x00B2, ("tips_get_flag", "") },
                { 0x00B3, ("tips_init", "") },
                { 0x00B4, ("tips_pause", "") },
                { 0x00B6, ("voice_play", "") },
                { 0x00B7, ("voice_stop", "") },
                { 0x00B8, ("voice_set_volume", "") },
                { 0x00B9, ("voice_get_volume", "") },
                { 0x00BA, ("set_voice_info", "") },
                { 0x00BB, ("voice_enable", "") },
                { 0x00BC, ("is_voice_enable", "") },
                { 0x00BD, (null, "") },
                { 0x00BE, ("bgv_play", "") },
                { 0x00BF, ("bgv_stop", "") },
                { 0x00C0, ("bgv_enable", "") },
                { 0x00C1, ("get_voice_ex_volume", "") },
                { 0x00C2, ("set_voice_ex_volume", "") },
                { 0x00C3, ("voice_check_enable", "") },
                { 0x00C4, ("voice_autopan_initialize", "") },
                { 0x00C5, ("voice_autopan_enable", "") },
                { 0x00C6, ("set_voice_autopan", "") },
                { 0x00C7, ("is_voice_autopan_enable", "") },
                { 0x00C8, ("voice_wait", "") },
                { 0x00C9, ("bgv_pause", "") },
                { 0x00CA, ("bgv_mute", "") },
                { 0x00CB, ("set_bgv_volume", "") },
                { 0x00CC, ("get_bgv_volume", "") },
                { 0x00CD, ("set_bgv_auto_volume", "") },
                { 0x00CE, ("voice_mute", "") },
                { 0x00CF, ("voice_call", "") },
                { 0x00D0, ("voice_call_clear", "") },
                { 0x00D2, ("wait", "") },
                { 0x00D3, ("wait_click", "") },
                { 0x00D4, ("wait_sync_begin", "") },
                { 0x00D5, ("wait_sync", "") },
                { 0x00D6, ("wait_sync_end", "") },
                { 0x00D7, (null, "") },
                { 0x00D8, ("wait_clear", "") },
                { 0x00D9, ("wait_click_no_anim", "") },
                { 0x00DA, ("wait_sync_get_time", "") },
                { 0x00DB, ("wait_time_push", "") },
                { 0x00DC, ("wait_time_pop", "") }
            };

        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly Dictionary<short, Action<List<Operand>>> _opcodeHandlers;
        private readonly Stack<Operand> _stack = new Stack<Operand>();

        public SoftpalDisassembler(Stream stream)
        {
            _stream = stream;
            _reader = new BinaryReader(stream);
            _opcodeHandlers = new Dictionary<short, Action<List<Operand>>>
                              {
                                  { 0x0017, HandleSyscallInstruction },
                                  { 0x001F, HandlePushInstruction },
                                  { 0x003A, HandleSelectChoiceInstruction },
                                  { 0x0088, HandleMessageInstruction },
                                  { 0x0095, HandleMessageInstruction },
                                  { 0x0096, HandleMessageInstruction },
                                  { 0x0097, HandleMessageInstruction },
                                  { 0x0098, HandleMessageInstruction },
                                  { 0x0099, HandleMessageInstruction }
                              };

            if (Encoding.ASCII.GetString(_reader.ReadBytes(4)) != "Sv20")
                throw new InvalidDataException("Invalid Softpal script magic");
        }

        public void Disassemble()
        {
            List<Operand> operands = new List<Operand>();

            _stream.Position = 0xC;
            while (_stream.Position < _stream.Length)
            {
                int opcode = _reader.ReadInt32();
                if ((opcode >> 16) != 1)
                    throw new InvalidDataException();

                (_, string operandTypes) = Opcodes[(short)opcode];
                var handler = _opcodeHandlers.GetOrDefault((short)opcode);
                if (handler == null)
                {
                    _reader.Skip(operandTypes.Length * 4);
                    _stack.Clear();
                    continue;
                }

                operands.Clear();
                foreach (char _ in operandTypes)
                {
                    int offset = (int)_stream.Position;
                    int value = _reader.ReadInt32();
                    operands.Add(new Operand(offset, value));
                }
                handler(operands);
            }
        }

        public event Action<int, ScriptStringType> TextAddressEncountered;

        private void HandlePushInstruction(List<Operand> operands)
        {
            if (operands[0].Value >> 28 == 0)
            {
                _stack.Push(operands[0]);
            }
            else
            {
                _stack.Clear();
            }
        }

        private void HandleSyscallInstruction(List<Operand> operands)
        {
            switch (operands[0].Value)
            {
                case 0x20002:
                case 0x2000F:
                case 0x20010:
                case 0x20011:
                case 0x20012:
                case 0x20013:
                    HandleMessageInstruction(null);
                    break;

                case 0x60002:
                    HandleSelectChoiceInstruction(null);
                    break;

                default:
                    _stack.Clear();
                    break;
            }
        }

        private void HandleMessageInstruction(List<Operand> operands)
        {
            if (_stack.Count < 4)
                return;

            _stack.Pop();
            Operand name = _stack.Pop();
            Operand text = _stack.Pop();

            if (name.Value != 0x0FFFFFFF)
                TextAddressEncountered?.Invoke(name.Offset, ScriptStringType.CharacterName);

            TextAddressEncountered?.Invoke(text.Offset, ScriptStringType.Message);

            _stack.Clear();
        }

        private void HandleSelectChoiceInstruction(List<Operand> operands)
        {
            if (_stack.Count < 1)
                return;

            Operand choice = _stack.Pop();
            TextAddressEncountered?.Invoke(choice.Offset, ScriptStringType.Message);
            _stack.Clear();
        }

        public void DisassembleToText(string filePath)
        {
            using Stream stream = File.Open(filePath, FileMode.Create, FileAccess.Write);
            using StreamWriter writer = new StreamWriter(stream);
            _stream.Position = 0xC;
            while (_stream.Position < _stream.Length)
            {
                writer.Write(_stream.Position.ToString("X08") + " ");

                int opcode = _reader.ReadInt32();
                if ((opcode >> 16) != 1)
                    throw new InvalidDataException();

                opcode &= 0xFFFF;
                (string opcodeName, string operandTypes) = Opcodes[(short)opcode];
                opcodeName ??= opcode.ToString("X04");

                writer.Write(opcodeName);
                foreach (char type in operandTypes)
                {
                    int value = _reader.ReadInt32();
                    writer.Write(" ");
                    writer.Write(type == 'p' ? $"[0x{value:X08}]" : $"0x{value:X}");
                }
                writer.WriteLine();
            }
        }

        private readonly struct Operand
        {
            public Operand(int offset, int value)
            {
                Offset = offset;
                Value = value;
            }

            public readonly int Offset;
            public readonly int Value;
        }
    }
}
