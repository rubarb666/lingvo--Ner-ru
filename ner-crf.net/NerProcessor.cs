﻿using System;
using System.Collections.Generic;

using lingvo.core;
using lingvo.tokenizing;

namespace lingvo.ner
{
    /// <summary>
    /// Обработчик именованных сущностей. Обработка с использованием библиотеки CRFSuit 
    /// </summary>
    public sealed class NerProcessor : IDisposable
    {
        #region [.private field's.]
        private const int DEFAULT_WORDSLIST_CAPACITY = 1000;
        private readonly Tokenizer                    _Tokenizer;
        private readonly List< word_t >               _Words;
		private readonly NerScriber                   _NerScriber;
        private Tokenizer.ProcessSentCallbackDelegate _ProcessSentCallback_1_Delegate;
        private Tokenizer.ProcessSentCallbackDelegate _ProcessSentCallback_2_Delegate;
        private Tokenizer.ProcessSentCallbackDelegate _OuterProcessSentCallback_Delegate;
        #endregion

        #region [.ctor().]
        public NerProcessor( NerProcessorConfig config )
		{
			CheckConfig( config );

            _NerScriber = NerScriber.Create( config.ModelFilename, config.TemplateFilename );
            _Tokenizer  = new Tokenizer( config.TokenizerConfig );
            _Words      = new List< word_t >( DEFAULT_WORDSLIST_CAPACITY );
            _ProcessSentCallback_1_Delegate = new Tokenizer.ProcessSentCallbackDelegate( ProcessSentCallback_1 );
            _ProcessSentCallback_2_Delegate = new Tokenizer.ProcessSentCallbackDelegate( ProcessSentCallback_2 );
        }

        public void Dispose()
        {
            _NerScriber.Dispose();
        }
        #endregion

        public List< word_t > Run( string text, bool splitBySmiles )
        {
            _Words.Clear();

            _Tokenizer.Run( text, splitBySmiles, _ProcessSentCallback_1_Delegate );

            return (_Words);
        }
        private void ProcessSentCallback_1( List< word_t > words )
        {
            _NerScriber.Run( words );

            switch ( words.Count )
            {
                case 0: return;
                case 1:
                {
                    var word = words[ 0 ];
                    if ( word.nerOutputType != NerOutputType.O )
                    {
                        _Words.Add( word );
                    }
                }
                return;
            }

            NerPostMerging.Run( words );

            for ( int i = 0, len = words.Count; i < len; i++ )
            {
                var word = words[ i ];
                if ( word.nerOutputType != NerOutputType.O )
                {
                    _Words.Add( word );
                }
            }
        }

        public void Run( string text, bool splitBySmiles, Tokenizer.ProcessSentCallbackDelegate processSentCallback )
        {
            _OuterProcessSentCallback_Delegate = processSentCallback;

            _Tokenizer.Run( text, splitBySmiles, _ProcessSentCallback_2_Delegate );

            _OuterProcessSentCallback_Delegate = null;
        }
        private void ProcessSentCallback_2( List< word_t > words )
        {
            _NerScriber.Run( words );            

            switch ( words.Count )
            {
                case 0: return;
                case 1:
                {
                    _Words.Clear();
                    var word = words[ 0 ];
                    if ( word.nerOutputType != NerOutputType.O )
                    {
                        _Words.Add( word );
                    }
                }
                #region [.callback result.]
                _OuterProcessSentCallback_Delegate( _Words );
                #endregion
                return;
            }

            NerPostMerging.Run( words );

            _Words.Clear();
            for ( int i = 0, len = words.Count; i < len; i++ )
            {
                var word = words[ i ];
                if ( word.nerOutputType != NerOutputType.O )
                {
                    _Words.Add( word );
                }
            }

            #region [.callback result.]
            _OuterProcessSentCallback_Delegate( _Words );
            #endregion
        }

        public List< word_t[] > Run_Debug( string text, bool splitBySmiles )
        {
            var wordsBySents = new List< word_t[] >();

            _Tokenizer.Run( text, splitBySmiles, (words) =>
            {
                _NerScriber.Run( words );

                wordsBySents.Add( words.ToArray() );
            });

            return (wordsBySents);
        }

		private static void CheckConfig( NerProcessorConfig config )
		{
			config.ThrowIfNull( "config" );
			config.ModelFilename   .ThrowIfNullOrWhiteSpace( "ModelFilename" );
			config.TemplateFilename.ThrowIfNullOrWhiteSpace( "TemplateFilename" );
            config.TokenizerConfig .ThrowIfNull( "TokenizerConfig" );
		}

        /*/// Проверить применимость шаблона к промежуточному результату
        /// @param crfTemplate - Шаблон
        /// @param sentences - Промежуточный результат
        private static void CheckTemplateAndResult( CRFTemplateFile crfTemplate, List< Sentence > sentences )
        {
            if ( sentences.Count > 0 )
            {
                var words = sentences[ 0 ].GetWords();
                if ( words.Count > 0 )
                {
                    var columnsCount = words[ 0 ].GetGraphematicCharacteristics().Count;
                    if ( columnsCount != crfTemplate.ColumnNames.Length )
                    {
                        throw (new Exception("Не совпадает количество столбцов указанное в шаблоне (" + crfTemplate.ColumnNames.Length +
                                             ") с количеством столбцов в обрабатываемом файле (" + columnsCount + ")"
                                            ));
                    }
                }
            }
        }*/
		
    }
}
