namespace Luxia.Audio.Analysis;

/// <summary>Transformée de Fourier rapide (radix 2, en place) sur des tableaux de réels et d'imaginaires.</summary>
internal sealed class Fft
{
    private readonly int _size;
    private readonly double[] _cos;
    private readonly double[] _sin;
    private readonly int[] _reverse;

    public Fft(int size)
    {
        if (size < 2 || (size & (size - 1)) != 0)
        {
            throw new ArgumentException("La taille doit être une puissance de deux.", nameof(size));
        }

        _size = size;
        _cos = new double[size / 2];
        _sin = new double[size / 2];
        for (var i = 0; i < size / 2; i++)
        {
            var angle = -2 * Math.PI * i / size;
            _cos[i] = Math.Cos(angle);
            _sin[i] = Math.Sin(angle);
        }

        _reverse = new int[size];
        var bits = (int)Math.Log2(size);
        for (var i = 0; i < size; i++)
        {
            var reversed = 0;
            for (var b = 0; b < bits; b++)
            {
                reversed |= ((i >> b) & 1) << (bits - 1 - b);
            }

            _reverse[i] = reversed;
        }
    }

    /// <summary>Taille de la transformée.</summary>
    public int Size => _size;

    /// <summary>Transformée directe, en place.</summary>
    public void Transform(double[] re, double[] im)
    {
        for (var i = 0; i < _size; i++)
        {
            var j = _reverse[i];
            if (j > i)
            {
                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }
        }

        for (var length = 2; length <= _size; length <<= 1)
        {
            var half = length / 2;
            var step = _size / length;
            for (var start = 0; start < _size; start += length)
            {
                for (var k = 0; k < half; k++)
                {
                    var c = _cos[k * step];
                    var s = _sin[k * step];
                    var a = start + k;
                    var b = a + half;
                    var tr = (re[b] * c) - (im[b] * s);
                    var ti = (re[b] * s) + (im[b] * c);
                    re[b] = re[a] - tr;
                    im[b] = im[a] - ti;
                    re[a] += tr;
                    im[a] += ti;
                }
            }
        }
    }
}
