﻿using Microsoft.ML.Data;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text; 

namespace n_vision
{
    class OnnxModelScorer
    {
        private readonly string modelLocation;
        private readonly MLContext mlContext;


        public OnnxModelScorer(string modelLocation, MLContext mlContext)
        {
            this.modelLocation = modelLocation;
            this.mlContext = mlContext;
        }

        public class ModelInput
        {
            [VectorType(10)]
            [ColumnName("input.1")]
            public float[] Features { get; set; }
        }

        private ITransformer LoadModel(string modelLocation, string[] outputColumnNames, string[] inputColumnNames)
        {
            Console.WriteLine("Read model");
            Console.WriteLine($"Model location: {modelLocation}");

            // Create IDataView from empty list to obtain input data schema
            var data = mlContext.Data.LoadFromEnumerable(new List<ModelInput>());

            // Define scoring pipeline
            var pipeline = mlContext.Transforms.ApplyOnnxModel(modelFile: modelLocation, outputColumnNames: outputColumnNames, inputColumnNames: inputColumnNames);

            // Fit scoring pipeline
            var model = pipeline.Fit(data);

            return model;
        }
        public class Prediction
        {
            [VectorType(6)]
            [ColumnName("31")]
            public float[] action { get; set; }
            [VectorType(1)]
            [ColumnName("34")]
            public float[] state { get; set; }
        }
        private IEnumerable<float> PredictDataUsingModel(IDataView testData, ITransformer model)
        {
            Console.WriteLine("");
            Console.WriteLine("=====Identify the objects in the images=====");
            Console.WriteLine("");

            IDataView scoredData = model.Transform(testData);

            IEnumerable<float[]> probabilities = scoredData.GetColumn<float[]>("31");
            var a = probabilities.ToList();
            a.Count.ToString();
            return a[0];
        }

        public IEnumerable<float> Score(IDataView data)
        {
            var model = LoadModel(modelLocation, new[] { "31", "34" }, new[] { "input.1" });

            return PredictDataUsingModel(data, model);
        }
    }
}