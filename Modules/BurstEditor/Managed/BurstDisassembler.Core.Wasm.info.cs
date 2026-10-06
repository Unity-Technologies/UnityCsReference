// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Burst.Editor
{
    internal partial class BurstDisassembler
    {
        internal class WasmInstructionInfo
        {
            internal static bool GetWasmInfo(string instructionName, out string instructionInfo)
            {
                var returnValue = true;

                switch (instructionName)
                {
                    case "if":
                        instructionInfo = "Executes a statement if the last item on the stack is true.";
                        break;
                    case "end":
                        instructionInfo = "Can be used to end a block, loop, if or else.";
                        break;
                    case "end_function":
                        instructionInfo = "Ends function.";
                        break;
                    case "block":
                        instructionInfo = "Creates a label that can later be branched out of with a br.";
                        break;
                    case "end_block":
                        instructionInfo = "Ends the previous opened block.";
                        break;
                    case "loop":
                        instructionInfo = "Creates a label that can later be branched to with a br.";
                        break;
                    case "end_loop":
                        instructionInfo = "Ends the previous opened loop label.";
                        break;
                    case "unreachable":
                        instructionInfo = "Denotes a point in code that should not be reachable.";
                        break;
                    case "nop":
                        instructionInfo = "Does nothing.";
                        break;
                    case "call":
                        instructionInfo = "Calls a function.";
                        break;
                    case "call_indirect":
                        instructionInfo = "Calls a function in a table.";
                        break;
                    case "drop":
                        instructionInfo = "Pops a value from the stack, and discards it.";
                        break;
                    case "select":
                        instructionInfo = "Selects one of its first two operands based on a boolean condition.";
                        break;
                    case "get":
                        instructionInfo = "Load the value of a variable onto the stack.";
                        break;
                    case "set":
                        instructionInfo = "Set the value of a variable.";
                        break;
                    case "tee":
                        instructionInfo = "Set the value of a variable and keep the value on the stack.";
                        break;
                    case "load":
                        instructionInfo = "Load a number from memory.";
                        break;
                    case "load8_s":
                        instructionInfo = "Load a signed 8-bit value from memory.";
                        break;
                    case "load8_u":
                        instructionInfo = "Load an unsigned 8-bit value from memory.";
                        break;
                    case "load16_s":
                        instructionInfo = "Load a signed 16-bit value from memory.";
                        break;
                    case "load16_u":
                        instructionInfo = "Load an unsigned 16-bit value from memory.";
                        break;
                    case "load32_s":
                        instructionInfo = "Load a signed 32-bit value from memory.";
                        break;
                    case "load32_u":
                        instructionInfo = "Load an unsigned 32-bit value from memory.";
                        break;
                    case "store":
                        instructionInfo = "Store a number in memory.";
                        break;
                    case "store8":
                        instructionInfo = "Store a 8-bit number in memory.";
                        break;
                    case "store16":
                        instructionInfo = "Store a 16-bit number in memory.";
                        break;
                    case "store32":
                        instructionInfo = "Store a 32-bit number in memory.";
                        break;
                    case "size":
                        instructionInfo = "Get the size of the memory instance.";
                        break;
                    case "grow":
                        instructionInfo = "Increase the size of the memory instance.";
                        break;
                    case "const":
                        instructionInfo = "Declare a constant number.";
                        break;
                    case "clz":
                        instructionInfo = "Count leading zeros in a numbers binary representation.";
                        break;
                    case "ctz":
                        instructionInfo = "Count trailing zeros in a numbers binary representation.";
                        break;
                    case "popcnt":
                        instructionInfo = "Count the number of '1' in a numbers binary representation.";
                        break;
                    case "add":
                        instructionInfo = "Add up two numbers.";
                        break;
                    case "sub":
                        instructionInfo = "Subtract one number from another number.";
                        break;
                    case "mul":
                        instructionInfo = "Multiply one number by another number.";
                        break;
                    case "div_s":
                        instructionInfo = "Divide two signed numbers.";
                        break;
                    case "div_u":
                        instructionInfo = "Divide two unsigned numbers.";
                        break;
                    case "rem_s":
                        instructionInfo = "Calculate the remainder left over when two signed integers are divided.";
                        break;
                    case "rem_u":
                        instructionInfo = "Calculate the remainder left over when two unsigned integers are divided.";
                        break;
                    case "and":
                        instructionInfo = "Bitwise and operation.";
                        break;
                    case "or":
                        instructionInfo = "Bitwise or operation.";
                        break;
                    case "xor":
                        instructionInfo = "Bitwise exclusive or operation.";
                        break;
                    case "shl":
                        instructionInfo = "Bitwise shift left operation.";
                        break;
                    case "shr_s":
                        instructionInfo = "Bitwise signed shift right operation.";
                        break;
                    case "shr_u":
                        instructionInfo = "Bitwise unsigned shift right operation.";
                        break;
                    case "rotl":
                        instructionInfo = "Bitwise rotate left operation.";
                        break;
                    case "rotr":
                        instructionInfo = "Bitwise rotate right operation.";
                        break;
                    case "abs":
                        instructionInfo = "Get the absolute value of a number.";
                        break;
                    case "neg":
                        instructionInfo = "Negate a number.";
                        break;
                    case "ceil":
                        instructionInfo = "Round up a number.";
                        break;
                    case "floor":
                        instructionInfo = "Round down a number.";
                        break;
                    case "trunc":
                        instructionInfo = "Discard the fractional part of a number.";
                        break;
                    case "sqrt":
                        instructionInfo = "Get the square root of a number.";
                        break;
                    case "div":
                        instructionInfo = "Divide two numbers.";
                        break;
                    case "min":
                        instructionInfo = "Get the lower of two numbers.";
                        break;
                    case "max":
                        instructionInfo = "Get the highest of two numbers.";
                        break;
                    case "copysign":
                        instructionInfo = "Copy just the sign bit from one number to another.";
                        break;
                    case "wrap_i64":
                        instructionInfo = "Convert (wrap) i64 number to i32 number.";
                        break;
                    case "trunc_f32_s":
                        instructionInfo = "Truncate fractional part away from a signed 32-bit floating number, giving a " +
                                          "signed 32-bit integer.";
                        break;
                    case "trunc_f32_u":
                        instructionInfo = "Truncate fractional part away from a unsigned 32-bit floating number, giving a " +
                                          "unsigned 32-bit integer.";
                        break;
                    case "trunc_f64_s":
                        instructionInfo = "Truncate fractional part away from a signed 64-bit floating number, giving a " +
                                          "signed 64-bit integer.";
                        break;
                    case "trunc_f64_u":
                        instructionInfo = "Truncate fractional part away from a unsigned 64-bit floating number, giving a " +
                                          "unsigned 64-bit integer.";
                        break;
                    case "extend_i32_s":
                        instructionInfo = "Convert (extend) signed 32-bit integer to signed 64-bit integer number.";
                        break;
                    case "extend_i32_u":
                        instructionInfo = "Convert (extend) unsigned 32-bit integer to unsigned 64-bit integer number.";
                        break;
                    case "convert_i32_s":
                        instructionInfo = "Convert signed 32-bit integer to signed 32-bit floating number.";
                        break;
                    case "convert_i32_u":
                        instructionInfo = "Convert unsigned 32-bit integer to unsigned 32-bit floating number.";
                        break;
                    case "convert_i64_s":
                        instructionInfo = "Convert signed 64-bit integer to signed 64-bit floating number.";
                        break;
                    case "convert_i64_u":
                        instructionInfo = "Convert unsigned 64-bit integer to unsigned 64-bit floating number.";
                        break;
                    case "demote_f64":
                        instructionInfo = "Convert (demote) 64-bit floating number to 32-bit floating number.";
                        break;
                    case "promote_f32":
                        instructionInfo = "Convert (promote) 32-bit floating number to 64-bit floating number.";
                        break;
                    case "reinterpret_f32":
                        instructionInfo = "Reinterpret the bytes of 32-bit floating number as 32-bit integer number.";
                        break;
                    case "reinterpret_f64":
                        instructionInfo = "Reinterpret the bytes of 64-bit floating number as 64-bit integer number.";
                        break;
                    case "reinterpret_i32":
                        instructionInfo = "Reinterpret the bytes of 32-bit integer number as 32-bit floating number.";
                        break;
                    case "reinterpret_i64":
                        instructionInfo = "Reinterpret the bytes of 64-bit integer number as 64-bit floating number.";
                        break;
                    case "br_if":
                        instructionInfo = "Branch to a loop or block if condition is true.";
                        break;
                    case "br":
                        instructionInfo = "Branch to a loop or block.";
                        break;
                    case "br_table":
                        instructionInfo = "Branch to a loop or block if condition based on argument.";
                        break;
                    case "return":
                        instructionInfo = "Returns from a function.";
                        break;
                    default:
                        instructionInfo = string.Empty;
                        break;
                }

                return returnValue;
            }
        }
    }
}
