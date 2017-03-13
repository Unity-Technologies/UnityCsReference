// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    // System.Numerics is not available
    internal class ComplexD
    {
        public double real;
        public double imag;

        public ComplexD(double real, double imag)
        {
            this.real = real;
            this.imag = imag;
        }

        static public ComplexD Add(ComplexD a, ComplexD b)
        {
            return new ComplexD(
                a.real + b.real,
                a.imag + b.imag);
        }

        static public ComplexD Add(ComplexD a, double b)
        {
            return new ComplexD(
                a.real + b,
                a.imag);
        }

        static public ComplexD Add(double a, ComplexD b)
        {
            return new ComplexD(
                a + b.real,
                b.imag);
        }

        static public ComplexD Sub(ComplexD a, ComplexD b)
        {
            return new ComplexD(
                a.real - b.real,
                a.imag - b.imag);
        }

        static public ComplexD Sub(ComplexD a, double b)
        {
            return new ComplexD(
                a.real - b,
                a.imag);
        }

        static public ComplexD Sub(double a, ComplexD b)
        {
            return new ComplexD(
                a - b.real,
                -b.imag);
        }

        static public ComplexD Mul(ComplexD a, ComplexD b)
        {
            return new ComplexD(
                a.real * b.real - a.imag * b.imag,
                a.real * b.imag + a.imag * b.real);
        }

        static public ComplexD Mul(ComplexD a, double b)
        {
            return new ComplexD(
                a.real * b,
                a.imag * b);
        }

        static public ComplexD Mul(double a, ComplexD b)
        {
            return new ComplexD(
                a * b.real,
                a * b.imag);
        }

        static public ComplexD Div(ComplexD a, ComplexD b)
        {
            double d = b.real * b.real + b.imag * b.imag;
            double s = 1.0 / d;
            return new ComplexD(
                (a.real * b.real + a.imag * b.imag) * s,
                (a.imag * b.real - a.real * b.imag) * s);
        }

        static public ComplexD Div(double a, ComplexD b)
        {
            double d = b.real * b.real + b.imag * b.imag;
            double s = a / d;
            return new ComplexD(
                s * b.real,
                -s * b.imag);
        }

        static public ComplexD Div(ComplexD a, double b)
        {
            double s = 1.0 / b;
            return new ComplexD(
                a.real * s,
                a.imag * s);
        }

        static public ComplexD Exp(double omega)
        {
            return new ComplexD(
                Math.Cos(omega),
                Math.Sin(omega));
        }

        static public ComplexD Pow(ComplexD a, double b)
        {
            double p = Math.Atan2(a.imag, a.real);
            double m = Math.Pow(a.Mag2(), b * 0.5f);
            return new ComplexD(
                m * Math.Cos(p * b),
                m * Math.Sin(p * b)
                );
        }

        public double Mag2() { return real * real + imag * imag; }
        public double Mag() { return Math.Sqrt(Mag2()); }

        public static ComplexD operator+(ComplexD a, ComplexD b) { return Add(a, b); }
        public static ComplexD operator-(ComplexD a, ComplexD b) { return Sub(a, b); }
        public static ComplexD operator*(ComplexD a, ComplexD b) { return Mul(a, b); }
        public static ComplexD operator/(ComplexD a, ComplexD b) { return Div(a, b); }
        public static ComplexD operator+(ComplexD a, double b) { return Add(a, b); }
        public static ComplexD operator-(ComplexD a, double b) { return Sub(a, b); }
        public static ComplexD operator*(ComplexD a, double b) { return Mul(a, b); }
        public static ComplexD operator/(ComplexD a, double b) { return Div(a, b); }
        public static ComplexD operator+(double a, ComplexD b) { return Add(a, b); }
        public static ComplexD operator-(double a, ComplexD b) { return Sub(a, b); }
        public static ComplexD operator*(double a, ComplexD b) { return Mul(a, b); }
        public static ComplexD operator/(double a, ComplexD b) { return Div(a, b); }
    }
}
