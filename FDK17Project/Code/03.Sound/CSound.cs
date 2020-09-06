using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using SlimDX;
using SlimDX.DirectSound;
using SlimDX.Multimedia;
using Un4seen.Bass;
using Un4seen.BassAsio;
using Un4seen.BassWasapi;
using Un4seen.Bass.AddOn.Mix;
using Un4seen.Bass.AddOn.Fx;
using DirectShowLib;

namespace FDK
{
	#region [ DTXMania用拡張 ]
	public class CSoundManager	// : CSound
	{
		public static ISoundDevice SoundDevice
		{
			get; set;
		}
		public static ESoundDeviceType SoundDeviceType
		{
			get; set;
		}
		public static CSoundTimer rcPerformanceTimer = null;

		public static IntPtr WindowHandle;

		public static bool bIsTimeStretch = false;
		public static int nMixing = 0;
		public int GetMixingStreams()
		{
			return nMixing;
		}
		public static int nStreams = 0;
		public int GetStreams()
		{
			return nStreams;
		}
		#region [ WASAPI/ASIO/DirectSound設定値 ]
		/// <summary>
		/// <para>WASAPI 排他モード出力における再生遅延[ms]（の希望値）。最終的にはこの数値を基にドライバが決定する）。</para>
		/// <para>→ WASAPI初期化時に自動設定するようにしたため、ここで設定した値は使用しないようになった。</para>
		/// </summary>
		public static int SoundDelayExclusiveWASAPI = 0;		// SSTでは、50ms
		public int GetSoundExclusiveWASAPI()
		{
			return SoundDelayExclusiveWASAPI;
		}
		public void SetSoundDelayExclusiveWASAPI( int value )
		{
			SoundDelayExclusiveWASAPI = value;
		}
		/// <summary>
		/// <para>WASAPI 共有モード出力における再生遅延[ms]。ユーザが決定する。</para>
		/// </summary>
		public static int SoundDelaySharedWASAPI = 100;
		/// <summary>
		/// <para>排他WASAPIバッファの更新間隔。出力間隔ではないので注意。</para>
		/// <para>→ 自動設定されるのでSoundDelay よりも小さい値であること。（小さすぎる場合はBASSによって自動修正される。）</para>
		/// </summary>
		public static int SoundUpdatePeriodExclusiveWASAPI = 6;
		/// <summary>
		/// <para>共有WASAPIバッファの更新間隔。出力間隔ではないので注意。</para>
		/// <para>SoundDelay よりも小さい値であること。（小さすぎる場合はBASSによって自動修正される。）</para>
		/// </summary>
		public static int SoundUpdatePeriodSharedWASAPI = 6;
		///// <summary>
		///// <para>ASIO 出力における再生遅延[ms]（の希望値）。最終的にはこの数値を基にドライバが決定する）。</para>
		///// </summary>
		//public static int SoundDelayASIO = 0;					// SSTでは50ms。0にすると、デバイスの設定値をそのまま使う。
		/// <summary>
		/// <para>ASIO 出力におけるバッファサイズ。</para>
		/// </summary>
		public static int SoundDelayASIO = 0;						// 0にすると、デバイスの設定値をそのまま使う。
		public int GetSoundDelayASIO()
		{
			return SoundDelayASIO;
		}
		public void SetSoundDelayASIO(int value)
		{
			SoundDelayASIO = value;
		}
		public static int ASIODevice = 0;
		public int GetASIODevice()
		{
			return ASIODevice;
		}
		public void SetASIODevice(int value)
		{
			ASIODevice = value;
		}
		/// <summary>
		/// <para>DirectSound 出力における再生遅延[ms]。ユーザが決定する。</para>
		/// </summary>
		public static int SoundDelayDirectSound = 100;

		public long GetSoundDelay()
		{
			if ( SoundDevice != null )
			{
				return SoundDevice.n実バッファサイズms;
			}
			else
			{
				return -1;
			}
		}

		#endregion


	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="handle"></param>
		public CSoundManager( IntPtr handle )	// #30803 従来のコンストラクタ相当のI/Fを追加。(DTXC用)
		{
			WindowHandle = handle;
			SoundDevice = null;
			t初期化( ESoundDeviceType.DirectSound, 0, 0, 0 );
		}
		public CSoundManager( IntPtr handle, ESoundDeviceType soundDeviceType, int nSoundDelayExclusiveWASAPI, int nSoundDelayASIO, int nASIODevice )
		{
			WindowHandle = handle;
			SoundDevice = null;
			t初期化( soundDeviceType, nSoundDelayExclusiveWASAPI, nSoundDelayASIO, nASIODevice );
		}
		public void Dispose()
		{
			t終了();
		}

		public static void t初期化()
		{
			t初期化( ESoundDeviceType.DirectSound, 0, 0, 0 );
		}


		public static void t初期化( ESoundDeviceType soundDeviceType, int _nSoundDelayExclusiveWASAPI, int _nSoundDelayASIO, int _nASIODevice )
		{
			//SoundDevice = null;						// 後で再初期化することがあるので、null初期化はコンストラクタに回す
			rcPerformanceTimer = null;						// Global.Bass 依存（つまりユーザ依存）
			nMixing = 0;

			SoundDelayExclusiveWASAPI = _nSoundDelayExclusiveWASAPI;
			SoundDelayASIO = _nSoundDelayASIO;
			ASIODevice = _nASIODevice;

			ESoundDeviceType[] ESoundDeviceTypes = new ESoundDeviceType[ 4 ]
			{
				ESoundDeviceType.ExclusiveWASAPI,
				ESoundDeviceType.ASIO,
				ESoundDeviceType.DirectSound,
				ESoundDeviceType.Unknown
			};

			int n初期デバイス;
			switch ( soundDeviceType )
			{
				case ESoundDeviceType.ExclusiveWASAPI:
					n初期デバイス = 0;
					break;
				case ESoundDeviceType.ASIO:
					n初期デバイス = 1;
					break;
				case ESoundDeviceType.DirectSound:
					n初期デバイス = 2;
					break;
				default:
					n初期デバイス = 3;
					break;
			}
			for ( SoundDeviceType = ESoundDeviceTypes[ n初期デバイス ]; ; SoundDeviceType = ESoundDeviceTypes[ ++n初期デバイス ] )
			{
				try
				{
					t現在のユーザConfigに従ってサウンドデバイスとすべての既存サウンドを再構築する();
					break;
				}
				catch ( Exception e )
				{
					Trace.TraceInformation( e.Message );
					if ( ESoundDeviceTypes[ n初期デバイス ] == ESoundDeviceType.Unknown )
					{
						throw new Exception( string.Format( "サウンドデバイスの初期化に失敗しました。" ) );
					}
				}
			}
            if ( soundDeviceType == ESoundDeviceType.ExclusiveWASAPI || soundDeviceType == ESoundDeviceType.ASIO )
			{
				//Bass.BASS_SetConfig( BASSConfig.BASS_CONFIG_UPDATETHREADS, 4 );
				//Bass.BASS_SetConfig( BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0 );

				Trace.TraceInformation( "BASS_CONFIG_UpdatePeriod=" + Bass.BASS_GetConfig( BASSConfig.BASS_CONFIG_UPDATEPERIOD ) );
				Trace.TraceInformation( "BASS_CONFIG_UpdateThreads=" + Bass.BASS_GetConfig( BASSConfig.BASS_CONFIG_UPDATETHREADS ) );
			}
		}


		public static void t終了()
		{
			CCommon.tDispose( SoundDevice ); SoundDevice = null;
			CCommon.tDispose( ref rcPerformanceTimer );	// Global.Bass を解放した後に解放すること。（Global.Bass で参照されているため）
		}


		public static void t現在のユーザConfigに従ってサウンドデバイスとすべての既存サウンドを再構築する()
		{
			#region [ すでにサウンドデバイスと演奏タイマが構築されていれば解放する。]
			//-----------------
			if ( SoundDevice != null )
			{
				// すでに生成済みのサウンドがあれば初期状態に戻す。

				CSound.tすべてのサウンドを初期状態に戻す();		// リソースは解放するが、CSoundのインスタンスは残す。


				// サウンドデバイスと演奏タイマを解放する。

				CCommon.tDispose( SoundDevice ); SoundDevice = null;
				CCommon.tDispose( ref rcPerformanceTimer );	// Global.SoundDevice を解放した後に解放すること。（Global.SoundDevice で参照されているため）
			}
			//-----------------
			#endregion

			#region [ 新しいサウンドデバイスを構築する。]
			//-----------------
			switch ( SoundDeviceType )
			{
				case ESoundDeviceType.ExclusiveWASAPI:
					SoundDevice = new CSoundDeviceWASAPI( CSoundDeviceWASAPI.Eデバイスモード.排他, SoundDelayExclusiveWASAPI, SoundUpdatePeriodExclusiveWASAPI );
					break;

				case ESoundDeviceType.SharedWASAPI:
					SoundDevice = new CSoundDeviceWASAPI( CSoundDeviceWASAPI.Eデバイスモード.共有, SoundDelaySharedWASAPI, SoundUpdatePeriodSharedWASAPI );
					break;

				case ESoundDeviceType.ASIO:
					SoundDevice = new CSoundDeviceASIO( SoundDelayASIO, ASIODevice );
					break;

				case ESoundDeviceType.DirectSound:
					SoundDevice = new CSoundDeviceDirectSound( WindowHandle, SoundDelayDirectSound );
					break;

				default:
					throw new Exception( string.Format( "未対応の SoundDeviceType です。[{0}]", SoundDeviceType.ToString() ) );
			}
			//-----------------
			#endregion
			#region [ 新しい演奏タイマを構築する。]
			//-----------------
			rcPerformanceTimer = new CSoundTimer( SoundDevice );
			//-----------------
			#endregion

			CSound.tすべてのサウンドを再構築する( SoundDevice );		// すでに生成済みのサウンドがあれば作り直す。
		}
		public CSound tGenerateSound( string filename )  // tサウンドを生成する
		{
			if ( SoundDeviceType == ESoundDeviceType.Unknown )
			{
				throw new Exception( string.Format( "未対応の SoundDeviceType です。[{0}]", SoundDeviceType.ToString() ) );
			}
			return SoundDevice.tサウンドを作成する( filename );
		}

        private static DateTime lastUpdateTime = DateTime.MinValue;
		public void t再生中の処理をする( object o )			// #26122 2011.9.1 yyagi; delegate経由の呼び出し用
		{
			t再生中の処理をする();
		}
		public void t再生中の処理をする()
		{
//★★★★★★★★★★★★★★★★★★★★★ダミー★★★★★★★★★★★★★★★★★★
//			Debug.Write( "再生中の処理をする()" );
            //DateTime now = DateTime.Now;
            //TimeSpan ts = now - lastUpdateTime;
            //if ( ts.Milliseconds > 5 )
            //{
            //    bool b = Bass.BASS_Update( 100 * 2 );
            //    lastUpdateTime = DateTime.Now;
            //    if ( !b )
            //    {
            //        Trace.TraceInformation( "BASS_UPdate() failed: " + Bass.BASS_ErrorGetCode().ToString() );
            //    }
            //}
		}

		public void tDiscard( CSound csound )
		{
			csound.tRelease( true );			// インスタンスは存続→破棄にする。
			csound = null;
		}

		public float GetCPUusage()
		{
			float f;
			switch ( SoundDeviceType )
			{
				case ESoundDeviceType.ExclusiveWASAPI:
				case ESoundDeviceType.SharedWASAPI:
					f = BassWasapi.BASS_WASAPI_GetCPU();
					break;
				case ESoundDeviceType.ASIO:
					f = BassAsio.BASS_ASIO_GetCPU();
					break;
				case ESoundDeviceType.DirectSound:
					f = 0.0f;
					break;
				default:
					f = 0.0f;
					break;
			}
			return f;
		}

		public string GetCurrentSoundDeviceType()
		{
			switch ( SoundDeviceType )
			{
				case ESoundDeviceType.ExclusiveWASAPI:
				case ESoundDeviceType.SharedWASAPI:
					return "WASAPI";
				case ESoundDeviceType.ASIO:
					return "ASIO";
				case ESoundDeviceType.DirectSound:
					return "DirectSound";
				default:
					return "Unknown";
			}
		}

		public void AddMixer( CSound cs, double db再生速度, bool _b演奏終了後も再生が続くチップである )
		{
			cs.b演奏終了後も再生が続くチップである = _b演奏終了後も再生が続くチップである;
			cs.dbPlaySpeed = db再生速度;
			cs.tBASSAddSoundToMixer();
		}
		public void AddMixer( CSound cs, double db再生速度 )
		{
			cs.dbPlaySpeed = db再生速度;
			cs.tBASSAddSoundToMixer();
		}
		public void AddMixer( CSound cs )
		{
			cs.tBASSAddSoundToMixer();
		}
		public void RemoveMixer( CSound cs )
		{
			cs.tBASSサウンドをミキサーから削除する();
		}
	}
	#endregion

	// CSound は、サウンドデバイスが変更されたときも、インスタンスを再作成することなく、新しいデバイスで作り直せる必要がある。
	// そのため、デバイスごとに別のクラスに分割するのではなく、１つのクラスに集約するものとする。

	public class CSound : IDisposable, ICloneable
	{
		#region [ DTXMania用拡張 ]
		public int nTotalPlayTimeMs
		{
			get;
			private set;
		}
		public int nサウンドバッファサイズ		// 取りあえず0固定★★★★★★★★★★★★★★★★★★★★
		{
			get { return 0; }
		}
		public bool bストリーム再生する			// 取りあえずfalse固定★★★★★★★★★★★★★★★★★★★★
												// trueにすると同一チップ音の多重再生で問題が出る(4POLY音源として動かない)
		{
			get { return false; }
		}
		public double db周波数倍率
		{
			get
			{
				return _db周波数倍率;
			}
			set
			{
				if ( _db周波数倍率 != value )
				{
					_db周波数倍率 = value;
					if ( bIsBASS )
					{
						Bass.BASS_ChannelSetAttribute( this.hBassStream, BASSAttribute.BASS_ATTRIB_FREQ, ( float ) ( _db周波数倍率 * _db再生速度 * nオリジナルの周波数 ) );
					}
					else
					{
//						if ( b再生中 )	// #30838 2012.2.24 yyagi (delete b再生中)
//						{
							this.Buffer.Frequency = ( int ) ( _db周波数倍率 * _db再生速度 * nオリジナルの周波数 );
//						}
					}
				}
			}
		}
		public double dbPlaySpeed
		{
			get
			{
				return _db再生速度;
			}
			set
			{
				if ( _db再生速度 != value )
				{
					_db再生速度 = value;
					bIs1倍速再生 = ( _db再生速度 == 1.000f );
					if ( bIsBASS )
					{
						if ( _hTempoStream != 0 && !this.bIs1倍速再生 )	// 再生速度がx1.000のときは、TempoStreamを用いないようにして高速化する
				        {
							this.hBassStream = _hTempoStream;
				        }
				        else
						{
							this.hBassStream = _hBassStream;
				        }

						if ( CSoundManager.bIsTimeStretch )
						{
							Bass.BASS_ChannelSetAttribute( this.hBassStream, BASSAttribute.BASS_ATTRIB_TEMPO, (float) ( dbPlaySpeed * 100 - 100 ) );
							//double seconds = Bass.BASS_ChannelBytes2Seconds( this.hTempoStream, nBytes );
							//this.nTotalPlayTimeMs = (int) ( seconds * 1000 );
						}
						else
						{
							Bass.BASS_ChannelSetAttribute( this.hBassStream, BASSAttribute.BASS_ATTRIB_FREQ, ( float ) ( _db周波数倍率 * _db再生速度 * nオリジナルの周波数 ) );
						}
					}
					else
					{
//						if ( b再生中 )	// #30838 2012.2.24 yyagi (delete b再生中)
//						{
							this.Buffer.Frequency = ( int ) ( _db周波数倍率 * _db再生速度 * nオリジナルの周波数 );
//						}
					}
				}
			}
		}
		#endregion

		public bool b演奏終了後も再生が続くチップである = false;	// これがtrueなら、本サウンドの再生終了のコールバック時に自動でミキサーから削除する

		private STREAMPROC _cbStreamXA;		// make it global, so that the GC can not remove it
		private SYNCPROC _cbEndofStream;	// ストリームの終端まで再生されたときに呼び出されるコールバック
//		private WaitCallback _cbRemoveMixerChannel;

		/// <summary>
		/// <para>0:最小～100:原音</para>
		/// </summary>
		public int nVolume  // n音量
		{
			get
			{
				if( this.bIsBASS )
				{
					float f音量 = 0.0f;
					if ( !Bass.BASS_ChannelGetAttribute( this.hBassStream, BASSAttribute.BASS_ATTRIB_VOL, ref f音量 ) )
					//if ( BassMix.BASS_Mixer_ChannelGetEnvelopePos( this.hBassStream, BASSMIXEnvelope.BASS_MIXER_ENV_VOL, ref f音量 ) == -1 )
					    return 100;
					return (int) ( f音量 * 100 );
				}
				else if( this.bIsDirectSound )
				{
					return this._n音量;
				}
				return -1;
			}
			set
			{
				if( this.bIsBASS )
				{
					float f音量 = Math.Min( Math.Max( value, 0 ), 100 ) / 100.0f;	// 0～100 → 0.0～1.0
					//var nodes = new BASS_MIXER_NODE[ 1 ] { new BASS_MIXER_NODE( 0, f音量 ) };
					//BassMix.BASS_Mixer_ChannelSetEnvelope( this.hBassStream, BASSMIXEnvelope.BASS_MIXER_ENV_VOL, nodes );
					Bass.BASS_ChannelSetAttribute( this.hBassStream, BASSAttribute.BASS_ATTRIB_VOL, f音量 );

				}
				else if( this.bIsDirectSound )
				{
					this._n音量 = value;

					if( this._n音量 == 0 )
					{
						this._n音量db = -10000;
					}
					else
					{
						this._n音量db = (int) ( ( 20.0 * Math.Log10( ( (double) this._n音量 ) / 100.0 ) ) * 100.0 );
					}

					this.Buffer.Volume = this._n音量db;
				}
			}
		}

		/// <summary>
		/// <para>左:-100～中央:0～100:右。set のみ。</para>
		/// </summary>
		public int nPosition  // n位置
		{
			get
			{
				if( this.bIsBASS )
				{
					float f位置 = 0.0f;
					if ( !Bass.BASS_ChannelGetAttribute( this.hBassStream, BASSAttribute.BASS_ATTRIB_PAN, ref f位置 ) )
						//if( BassMix.BASS_Mixer_ChannelGetEnvelopePos( this.hBassStream, BASSMIXEnvelope.BASS_MIXER_ENV_PAN, ref f位置 ) == -1 )
						return 0;
					return (int) ( f位置 * 100 );
				}
				else if( this.bIsDirectSound )
				{
					return this._n位置;
				}
				return -9999;
			}
			set
			{
				if( this.bIsBASS )
				{
					float f位置 = Math.Min( Math.Max( value, -100 ), 100 ) / 100.0f;	// -100～100 → -1.0～1.0
					//var nodes = new BASS_MIXER_NODE[ 1 ] { new BASS_MIXER_NODE( 0, f位置 ) };
					//BassMix.BASS_Mixer_ChannelSetEnvelope( this.hBassStream, BASSMIXEnvelope.BASS_MIXER_ENV_PAN, nodes );
					Bass.BASS_ChannelSetAttribute( this.hBassStream, BASSAttribute.BASS_ATTRIB_PAN, f位置 );
				}
				else if( this.bIsDirectSound )
				{
					this._n位置 = Math.Min( Math.Max( -100, value ), 100 );		// -100～100

					if( this._n位置 == 0 )
					{
						this._n位置db = 0;
					}
					else if( this._n位置 == -100 )
					{
						this._n位置db = -10000;
					}
					else if( this._n位置 == 100 )
					{
						this._n位置db = 10000;
					}
					else if( this._n位置 < 0 )
					{
						this._n位置db = (int) ( ( 20.0 * Math.Log10( ( (double) ( this._n位置 + 100 ) ) / 100.0 ) ) * 100.0 );
					}
					else
					{
						this._n位置db = (int) ( ( -20.0 * Math.Log10( ( (double) ( 100 - this._n位置 ) ) / 100.0 ) ) * 100.0 );
					}

					this.Buffer.Pan = this._n位置db;
				}
			}
		}

		/// <summary>
		/// <para>DirectSoundのセカンダリバッファ。</para>
		/// </summary>
		//public SecondarySoundBuffer DirectSoundBuffer
		public SoundBuffer DirectSoundBuffer
		{
			get { return this.Buffer; }
		}

		/// <summary>
		/// <para>DirectSoundのセカンダリバッファ作成時のフラグ。</para>
		/// </summary>
		public BufferFlags DirectSoundBufferFlags
		{
			get;
			protected set;
		}

		/// <summary>
		/// <para>全インスタンスリスト。</para>
		/// <para>～を作成する() で追加され、tRelease() or Dispose() で解放される。</para>
		/// </summary>
		public static List<CSound> listインスタンス = new List<CSound>();

		public static void ShowAllCSoundFiles()
		{
			int i = 0;
			foreach ( CSound cs in listインスタンス )
			{
				Debug.WriteLine( i++.ToString( "d3" ) + ": " + Path.GetFileName( cs.strファイル名 ) );
			}
		}

		public CSound()
		{
			this.nVolume = 100;
			this.nPosition = 0;
			this._db周波数倍率 = 1.0;
			this._db再生速度 = 1.0;
			this.DirectSoundBufferFlags = CSoundDeviceDirectSound.DefaultFlags;
//			this._cbRemoveMixerChannel = new WaitCallback( RemoveMixerChannelLater );
			this._hBassStream = -1;
			this._hTempoStream = 0;
		}

		public object Clone()
		{
			if ( !bIsDirectSound )
			{
				throw new NotImplementedException();
			}
			CSound clone = (CSound) MemberwiseClone();	// これだけだとCY連打が途切れる＆タイトルに戻る際にNullRef例外発生
			this.DirectSound.DuplicateSoundBuffer( this.Buffer, out clone.Buffer );

			// CSound.listインスタンス.Add( this );			// インスタンスリストに登録。
			// 本来これを加えるべきだが、Add後Removeできなくなっている。Clone()の仕方の問題であろう。

			return clone;
		}
		public void tASIOサウンドを作成する( string strファイル名, int hMixer )
		{
			this.tBASSサウンドを作成する( strファイル名, hMixer, BASSFlag.BASS_STREAM_DECODE );
			this.eデバイス種別 = ESoundDeviceType.ASIO;		// 作成後に設定する。（作成に失敗してると例外発出されてここは実行されない）
		}
		public void tASIOサウンドを作成する( byte[] byArrWAVファイルイメージ, int hMixer )
		{
			this.tBASSサウンドを作成する( byArrWAVファイルイメージ, hMixer, BASSFlag.BASS_STREAM_DECODE );
			this.eデバイス種別 = ESoundDeviceType.ASIO;		// 作成後に設定する。（作成に失敗してると例外発出されてここは実行されない）
		}
		public void tWASAPIサウンドを作成する( string strファイル名, int hMixer, ESoundDeviceType eデバイス種別 )
		{
			this.tBASSサウンドを作成する( strファイル名, hMixer, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT );
			this.eデバイス種別 = eデバイス種別;		// 作成後に設定する。（作成に失敗してると例外発出されてここは実行されない）
		}
		public void tWASAPIサウンドを作成する( byte[] byArrWAVファイルイメージ, int hMixer, ESoundDeviceType eデバイス種別 )
		{
			this.tBASSサウンドを作成する( byArrWAVファイルイメージ, hMixer, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT );
			this.eデバイス種別 = eデバイス種別;		// 作成後に設定する。（作成に失敗してると例外発出されてここは実行されない）
		}
		public void tDirectSoundサウンドを作成する( string strファイル名, DirectSound DirectSound )
		{
			this.e作成方法 = E作成方法.ファイルから;
			this.strファイル名 = strファイル名;

			if ( String.Compare( Path.GetExtension( strファイル名 ), ".xa", true ) == 0 ||
				 String.Compare( Path.GetExtension( strファイル名 ), ".mp3", true ) == 0 ||
				 String.Compare( Path.GetExtension( strファイル名 ), ".ogg", true ) == 0 )	// caselessで文字列比較
			{
				tDirectSoundサウンドを作成するXaOggMp3( strファイル名, DirectSound );
				return;
			}

			// すべてのファイルを DirectShow でデコードすると時間がかかるので、ファイルが WAV かつ PCM フォーマットでない場合のみ DirectShow でデコードする。

			byte[] byArrWAVファイルイメージ = null;
			bool bファイルがWAVかつPCMフォーマットである = true;

			{
				#region [ ファイルがWAVかつPCMフォーマットか否か調べる。]
				//-----------------
				try
				{
					using ( var ws = new WaveStream( strファイル名 ) )
					{
						if ( ws.Format.FormatTag != WaveFormatTag.Pcm )
							bファイルがWAVかつPCMフォーマットである = false;
					}
				}
				catch
				{
					bファイルがWAVかつPCMフォーマットである = false;
				}
				//-----------------
				#endregion

				if ( bファイルがWAVかつPCMフォーマットである )
				{
					#region [ ファイルを読み込んで byArrWAVファイルイメージへ格納。]
					//-----------------
					var fs = File.Open( strファイル名, FileMode.Open, FileAccess.Read );
					var br = new BinaryReader( fs );

					byArrWAVファイルイメージ = new byte[ fs.Length ];
					br.Read( byArrWAVファイルイメージ, 0, (int) fs.Length );

					br.Close();
					fs.Close();
					//-----------------
					#endregion
				}
				else
				{
					#region [ DirectShow でデコード変換し、 byArrWAVファイルイメージへ格納。]
					//-----------------
					CDStoWAVFileImage.t変換( strファイル名, out byArrWAVファイルイメージ );
					//-----------------
					#endregion
				}
			}

			// あとはあちらで。

			this.tDirectSoundサウンドを作成する( byArrWAVファイルイメージ, DirectSound );
		}
		public void tDirectSoundサウンドを作成するXaOggMp3( string strファイル名, DirectSound DirectSound )
		{
			this.e作成方法 = E作成方法.ファイルから;
			this.strファイル名 = strファイル名;


			WaveFormat wfx = new WaveFormat();
			int nPCMデータの先頭インデックス = 0;
//			int nPCMサイズbyte = (int) ( xa.xaheader.nSamples * xa.xaheader.nChannels * 2 );	// nBytes = Bass.BASS_ChannelGetLength( this.hBassStream );

			int nPCMサイズbyte;
			CWin32.WAVEFORMATEX cw32wfx;
			tオンメモリ方式でデコードする( strファイル名, out this.byArrWAVファイルイメージ,
			out nPCMデータの先頭インデックス, out nPCMサイズbyte, out cw32wfx );

			wfx.AverageBytesPerSecond = (int) cw32wfx.nAvgBytesPerSec;
			wfx.BitsPerSample = (short) cw32wfx.wBitsPerSample;
			wfx.BlockAlignment = (short) cw32wfx.nBlockAlign;
			wfx.Channels = (short) cw32wfx.nChannels;
			wfx.FormatTag = WaveFormatTag.Pcm;	// xa.waveformatex.wFormatTag;
			wfx.SamplesPerSecond = (int) cw32wfx.nSamplesPerSec;

			// セカンダリバッファを作成し、PCMデータを書き込む。
			tDirectSoundサウンドを作成する_セカンダリバッファの作成とWAVデータ書き込み
				( ref this.byArrWAVファイルイメージ, DirectSound, CSoundDeviceDirectSound.DefaultFlags, wfx,
				  nPCMサイズbyte, nPCMデータの先頭インデックス );
		}

		public void tDirectSoundサウンドを作成する( byte[] byArrWAVファイルイメージ, DirectSound DirectSound )
		{
			this.tDirectSoundサウンドを作成する(  byArrWAVファイルイメージ, DirectSound, CSoundDeviceDirectSound.DefaultFlags );
		}
		public void tDirectSoundサウンドを作成する( byte[] byArrWAVファイルイメージ, DirectSound DirectSound, BufferFlags flags )
		{
			if( this.e作成方法 == E作成方法.Unknown )
				this.e作成方法 = E作成方法.WAVファイルイメージから;

			WaveFormat wfx = null;
			int nPCMデータの先頭インデックス = -1;
			int nPCMサイズbyte = -1;
	
			#region [ byArrWAVファイルイメージ[] から上記３つのデータを取得。]
			//-----------------
			var ms = new MemoryStream( byArrWAVファイルイメージ );
			var br = new BinaryReader( ms );

			try
			{
				// 'RIFF'＋RIFFデータサイズ

				if( br.ReadUInt32() != 0x46464952 )
					throw new InvalidDataException( "RIFFファイルではありません。" );
				br.ReadInt32();

				// 'WAVE'
				if( br.ReadUInt32() != 0x45564157 )
					throw new InvalidDataException( "WAVEファイルではありません。" );

				// チャンク
				while( ( ms.Position + 8 ) < ms.Length )	// +8 は、チャンク名＋チャンクサイズ。残り8バイト未満ならループ終了。
				{
					uint chunkName = br.ReadUInt32();

					// 'fmt '
					if( chunkName == 0x20746D66 )
					{
						long chunkSize = (long) br.ReadUInt32();

						var tag = (WaveFormatTag) br.ReadUInt16();

						if( tag == WaveFormatTag.Pcm ) wfx = new WaveFormat();
						else if( tag == WaveFormatTag.Extensible ) wfx = new SlimDX.Multimedia.WaveFormatExtensible();	// このクラスは WaveFormat を継承している。
						else
							throw new InvalidDataException( string.Format( "未対応のWAVEフォーマットタグです。(Tag:{0})", tag.ToString() ) );

						wfx.FormatTag = tag;
						wfx.Channels = br.ReadInt16();
						wfx.SamplesPerSecond = br.ReadInt32();
						wfx.AverageBytesPerSecond = br.ReadInt32();
						wfx.BlockAlignment = br.ReadInt16();
						wfx.BitsPerSample = br.ReadInt16();

						long nフォーマットサイズbyte = 16;

						if( wfx.FormatTag == WaveFormatTag.Extensible )
						{
							br.ReadUInt16();	// 拡張領域サイズbyte
							var wfxEx = (SlimDX.Multimedia.WaveFormatExtensible) wfx;
							wfxEx.ValidBitsPerSample = br.ReadInt16();
							wfxEx.ChannelMask = (Speakers) br.ReadInt32();
							wfxEx.SubFormat = new Guid( br.ReadBytes( 16 ) );	// GUID は 16byte (128bit)

							nフォーマットサイズbyte += 24;
						}

						ms.Seek( chunkSize - nフォーマットサイズbyte, SeekOrigin.Current );
						continue;
					}

					// 'data'
					else if( chunkName == 0x61746164 )
					{
						nPCMサイズbyte = br.ReadInt32();
						nPCMデータの先頭インデックス = (int) ms.Position;

						ms.Seek( nPCMサイズbyte, SeekOrigin.Current );
						continue;
					}

					// その他
					else
					{
						long chunkSize = (long) br.ReadUInt32();
						ms.Seek( chunkSize, SeekOrigin.Current );
						continue;
					}
				}

				if( wfx == null )
					throw new InvalidDataException( "fmt チャンクが存在しません。不正なサウンドデータです。" );
				if( nPCMサイズbyte < 0 )
					throw new InvalidDataException( "data チャンクが存在しません。不正なサウンドデータです。" );
			}
			finally
			{
				ms.Close();
				br.Close();
			}
			//-----------------
			#endregion


			// セカンダリバッファを作成し、PCMデータを書き込む。
			tDirectSoundサウンドを作成する_セカンダリバッファの作成とWAVデータ書き込み(
				ref byArrWAVファイルイメージ, DirectSound, flags, wfx, nPCMサイズbyte, nPCMデータの先頭インデックス );
		}

		private void tDirectSoundサウンドを作成する_セカンダリバッファの作成とWAVデータ書き込み
			( ref byte[] byArrWAVファイルイメージ, DirectSound DirectSound, BufferFlags flags, WaveFormat wfx,
			int nPCMサイズbyte, int nPCMデータの先頭インデックス )
		{
			// セカンダリバッファを作成し、PCMデータを書き込む。

			this.Buffer = new SecondarySoundBuffer( DirectSound, new SoundBufferDescription()
			{
				Format = ( wfx.FormatTag == WaveFormatTag.Pcm ) ? wfx : (SlimDX.Multimedia.WaveFormatExtensible) wfx,
				Flags = flags,
				SizeInBytes = nPCMサイズbyte,
			} );
			this.Buffer.Write( byArrWAVファイルイメージ, nPCMデータの先頭インデックス, nPCMサイズbyte, 0, LockFlags.None );

			// 作成完了。

			this.eデバイス種別 = ESoundDeviceType.DirectSound;
			this.DirectSoundBufferFlags = flags;
			this.byArrWAVファイルイメージ = byArrWAVファイルイメージ;
			this.DirectSound = DirectSound;

			// DTXMania用に追加
			this.nオリジナルの周波数 = wfx.SamplesPerSecond;
			nTotalPlayTimeMs = (int) ( ( (double) nPCMサイズbyte ) / ( this.Buffer.Format.AverageBytesPerSecond * 0.001 ) );


			// インスタンスリストに登録。

			CSound.listインスタンス.Add( this );
		}

		#region [ DTXMania用の変換 ]

		public void tサウンドを破棄する( CSound cs )
		{
			cs.tRelease();
		}
		public void tStartPlaying()  // t再生を開始する
		{
			tSetPlaybackPositionToBeginning();
			tPlaySound();
		}
		public void tStartPlaying( bool bループする )
		{
			if ( bIsBASS )
			{
				if ( bループする )
				{
					Bass.BASS_ChannelFlags( this.hBassStream, BASSFlag.BASS_SAMPLE_LOOP, BASSFlag.BASS_SAMPLE_LOOP );
				}
				else
				{
					Bass.BASS_ChannelFlags( this.hBassStream, BASSFlag.BASS_DEFAULT, BASSFlag.BASS_DEFAULT );
				}
			}
			tSetPlaybackPositionToBeginning();
			tPlaySound( bループする );
		}
		public void tStopPlayback()  // t再生を停止する
		{
			tStopSound();
			tSetPlaybackPositionToBeginning();
		}
		public void tPausePlayback()
		{
			tStopSound(true);
			this.n一時停止回数++;
		}
		public void tResumePlayback( long t)   // t再生を再開する ★★★★★★★★★★★★★★★★★★★★★★★★★★★★
		{
			Debug.WriteLine( "t再生を再開する(long " + t + ")" );
			tChangePlaybackPosition( t );
			tPlaySound();
			this.n一時停止回数--;
		}
		public bool b一時停止中
		{
			get
			{
				if ( this.bIsBASS )
				{
					bool ret = ( BassMix.BASS_Mixer_ChannelIsActive( this.hBassStream ) == BASSActive.BASS_ACTIVE_PAUSED ) &
								( BassMix.BASS_Mixer_ChannelGetPosition( this.hBassStream ) > 0 );
					return ret;
				}
				else
				{
					return ( this.n一時停止回数 > 0 );
				}
			}
		}
		public bool b再生中
		{
			get
			{
				if ( this.eデバイス種別 == ESoundDeviceType.DirectSound )
				{
					return ( ( this.Buffer.Status & BufferStatus.Playing ) != BufferStatus.None );
				}
				else
				{
					// 基本的にはBASS_ACTIVE_PLAYINGなら再生中だが、最後まで再生しきったchannelも
					// BASS_ACTIVE_PLAYINGのままになっているので、小細工が必要。
					bool ret = ( BassMix.BASS_Mixer_ChannelIsActive( this.hBassStream ) == BASSActive.BASS_ACTIVE_PLAYING );
					if ( BassMix.BASS_Mixer_ChannelGetPosition( this.hBassStream ) >= nBytes )
					{
						ret = false;
					}
					return ret;
				}
			}
		}
		//public lint t時刻から位置を返す( long t )
		//{
		//    double num = ( n時刻 * this.dbPlaySpeed ) * this.db周波数倍率;
		//    return (int) ( ( num * 0.01 ) * this.nSamplesPerSecond );
		//}
		#endregion


		public void tRelease()
		{
			tRelease( false );
		}

		public void tRelease( bool _bインスタンス削除 )
		{
			if ( this.bIsBASS )		// stream数の削減用
			{
				tBASSサウンドをミキサーから削除する();
				_cbEndofStream = null;
				//_cbStreamXA = null;
				CSoundManager.nStreams--;
			}
			bool bManagedも解放する = true;
			bool bインスタンス削除 = _bインスタンス削除;	// CSoundの再初期化時は、インスタンスは存続する。
			this.Dispose( bManagedも解放する, bインスタンス削除 );
//Debug.WriteLine( "Disposed: " + _bインスタンス削除 + " : " + Path.GetFileName( this.strファイル名 ) );
		}
		public void tPlaySound()  // tサウンドを再生する
		{
			tPlaySound( false );
		}
		public void tPlaySound( bool bループする)  // tサウンドを再生する
		{
			if ( this.bIsBASS )			// BASSサウンド時のループ処理は、tStartPlaying()側に実装。ここでは「bループする」は未使用。
			{
//Debug.WriteLine( "再生中?: " +  System.IO.Path.GetFileName(this.strファイル名) + " status=" + BassMix.BASS_Mixer_ChannelIsActive( this.hBassStream ) + " current=" + BassMix.BASS_Mixer_ChannelGetPosition( this.hBassStream ) + " nBytes=" + nBytes );
				bool b = BassMix.BASS_Mixer_ChannelPlay( this.hBassStream );
				if ( !b )
				{
//Debug.WriteLine( "再生しようとしたが、Mixerに登録されていなかった: " + Path.GetFileName( this.strファイル名 ) + ", stream#=" + this.hBassStream + ", ErrCode=" + Bass.BASS_ErrorGetCode() );

					bool bb = tBASSAddSoundToMixer();
					if ( !bb )
					{
Debug.WriteLine( "Mixerへの登録に失敗: " + Path.GetFileName( this.strファイル名 ) + ", ErrCode=" + Bass.BASS_ErrorGetCode() );
					}
					else
					{
//Debug.WriteLine( "Mixerへの登録に成功: " + Path.GetFileName( this.strファイル名 ) + ": " + Bass.BASS_ErrorGetCode() );
					}
					//this.tSetPlaybackPositionToBeginning();

					bool bbb = BassMix.BASS_Mixer_ChannelPlay( this.hBassStream );
					if (!bbb)
					{
Debug.WriteLine("更に再生に失敗: " + Path.GetFileName(this.strファイル名) + ", ErrCode=" + Bass.BASS_ErrorGetCode() );
					}
					else
					{
//						Debug.WriteLine("再生成功(ミキサー追加後)                       : " + Path.GetFileName(this.strファイル名));
					}
				}
				else
				{
//Debug.WriteLine( "再生成功: " + Path.GetFileName( this.strファイル名 ) + " (" + hBassStream + ")" );
				}
			}
			else if( this.bIsDirectSound )
			{
				PlayFlags pf = ( bループする ) ? PlayFlags.Looping : PlayFlags.None;
				this.Buffer.Play( 0, pf );
			}
		}
		public void tサウンドを先頭から再生する()
		{
			this.tSetPlaybackPositionToBeginning();
			this.tPlaySound();
		}
		public void tStopSound()
		{
			tStopSound( false );
		}
		public void tStopSound( bool pause )
		{
			if( this.bIsBASS )
			{
//Debug.WriteLine( "停止: " + System.IO.Path.GetFileName( this.strファイル名 ) + " status=" + BassMix.BASS_Mixer_ChannelIsActive( this.hBassStream ) + " current=" + BassMix.BASS_Mixer_ChannelGetPosition( this.hBassStream ) + " nBytes=" + nBytes );
				BassMix.BASS_Mixer_ChannelPause( this.hBassStream );
				if ( !pause )
				{
			//		tBASSサウンドをミキサーから削除する();		// PAUSEと再生停止を区別できるようにすること!!
				}
			}
			else if( this.bIsDirectSound )
			{
                try
                {
				    this.Buffer.Stop();
                }
                catch( Exception )
                {
                    // WASAPI/ASIOとDirectSoundを同時使用すると、Bufferがlostしてここで例外発生する。→ catchして無視する。
                    // DTXCからDTXManiaを呼び出すと、DTXC終了時にこの現象が発生する。
                }
			}
		}
		
		public void tSetPlaybackPositionToBeginning()
		{
			if( this.bIsBASS )
			{
				BassMix.BASS_Mixer_ChannelSetPosition( this.hBassStream, 0 );
				pos = 0;
			}
			else if( this.bIsDirectSound )
			{
				this.Buffer.CurrentPlayPosition = 0;
			}
		}
		public void tChangePlaybackPosition( long n位置ms )
		{
			if( this.bIsBASS )
			{
				BassMix.BASS_Mixer_ChannelSetPosition( this.hBassStream, Bass.BASS_ChannelSeconds2Bytes( this.hBassStream, n位置ms * this.db周波数倍率 * this.dbPlaySpeed / 1000.0 ), BASSMode.BASS_POS_BYTES );
			}
			else if( this.bIsDirectSound )
			{
				int n位置sample = (int) ( this.Buffer.Format.SamplesPerSecond * n位置ms * 0.001 * _db周波数倍率 * _db再生速度 );	// #30839 2013.2.24 yyagi; add _db周波数倍率 and _db再生速度
				this.Buffer.CurrentPlayPosition = n位置sample * this.Buffer.Format.BlockAlignment;
			}
		}

		public static void tすべてのサウンドを初期状態に戻す()
		{
			foreach ( var sound in CSound.listインスタンス )
			{
				sound.tRelease( false );
			}
		}
		public static void tすべてのサウンドを再構築する( ISoundDevice device )
		{
			if( CSound.listインスタンス.Count == 0 )
				return;


			// サウンドを再生する際にインスタンスリストも更新されるので、配列にコピーを取っておき、リストはクリアする。

			var sounds = CSound.listインスタンス.ToArray();
			CSound.listインスタンス.Clear();
			

			// 配列に基づいて個々のサウンドを作成する。

			for( int i = 0; i < sounds.Length; i++ )
			{
				switch( sounds[ i ].e作成方法 )
				{
					#region [ ファイルから ]
					case E作成方法.ファイルから:
						string strファイル名 = sounds[ i ].strファイル名;
						sounds[ i ].Dispose( true, false );
						device.tサウンドを作成する( strファイル名, ref sounds[ i ] );
						break;
					#endregion
					#region [ WAVファイルイメージから ]
					case E作成方法.WAVファイルイメージから:
						if( sounds[ i ].bIsBASS )
						{
							byte[] byArrWaveファイルイメージ = sounds[ i ].byArrWAVファイルイメージ;
							sounds[ i ].Dispose( true, false );
							device.tサウンドを作成する( byArrWaveファイルイメージ, ref sounds[ i ] );
						}
						else if( sounds[ i ].bIsDirectSound )
						{
							byte[] byArrWaveファイルイメージ = sounds[ i ].byArrWAVファイルイメージ;
							var flags = sounds[ i ].DirectSoundBufferFlags;
							sounds[ i ].Dispose( true, false );
							( (CSoundDeviceDirectSound) device ).tサウンドを作成する( byArrWaveファイルイメージ, flags, ref sounds[ i ] );
						}
						break;
					#endregion
				}
			}
		}

		#region [ Dispose-Finalizeパターン実装 ]
		//-----------------
		public void Dispose()
		{
			this.Dispose( true, true );
			GC.SuppressFinalize( this );
		}
		protected void Dispose( bool bManagedも解放する, bool bインスタンス削除 )
		{
			if( this.bIsBASS )
			{
				#region [ ASIO, WASAPI の解放 ]
				//-----------------
				if ( _hTempoStream != 0 )
				{
					BassMix.BASS_Mixer_ChannelRemove( this._hTempoStream );
					Bass.BASS_StreamFree( this._hTempoStream );
				}
				BassMix.BASS_Mixer_ChannelRemove( this._hBassStream );
				Bass.BASS_StreamFree( this._hBassStream );
				this.hBassStream = -1;
				this._hBassStream = -1;
				this._hTempoStream = 0;
				//-----------------
				#endregion
			}

			if( bManagedも解放する )
			{
				//int freeIndex = -1;

				//if ( CSound.listインスタンス != null )
				//{
				//    freeIndex = CSound.listインスタンス.IndexOf( this );
				//    if ( freeIndex == -1 )
				//    {
				//        Debug.WriteLine( "ERR: freeIndex==-1 : Count=" + CSound.listインスタンス.Count + ", filename=" + Path.GetFileName( this.strファイル名 ) );
				//    }
				//}

				if( this.eデバイス種別 == ESoundDeviceType.DirectSound )
				{
					#region [ DirectSound の解放 ]
					//-----------------
					if( this.Buffer != null )
					{
						try
						{
							this.Buffer.Stop();
						}
						catch
						{
							// 演奏終了後、長時間解放しないでいると、たまに AccessViolationException が発生することがある。
						}
						CCommon.tDispose( ref this.Buffer );
					}
					//-----------------
					#endregion
				}

				if( this.e作成方法 == E作成方法.WAVファイルイメージから &&
					this.eデバイス種別 != ESoundDeviceType.DirectSound )	// DirectSound は hGC 未使用。
				{
					if ( this.hGC != null && this.hGC.IsAllocated )
					{
						this.hGC.Free();
						this.hGC = default( GCHandle );
					}
				}
				if ( this.byArrWAVファイルイメージ != null )
				{
					this.byArrWAVファイルイメージ = null;
				}

				if ( bインスタンス削除 )
				{
					//try
					//{
					//    CSound.listインスタンス.RemoveAt( freeIndex );
					//}
					//catch
					//{
					//    Debug.WriteLine( "FAILED to remove CSound.listインスタンス: Count=" + CSound.listインスタンス.Count + ", filename=" + Path.GetFileName( this.strファイル名 ) );
					//}
					bool b = CSound.listインスタンス.Remove( this );	// これだと、Clone()したサウンドのremoveに失敗する
					if ( !b )
					{
						Debug.WriteLine( "FAILED to remove CSound.listインスタンス: Count=" + CSound.listインスタンス.Count + ", filename=" + Path.GetFileName( this.strファイル名 ) );
					}

				}
			}
		}
		~CSound()
		{
			this.Dispose( false, true );
		}
		//-----------------
		#endregion

		#region [ protected ]
		//-----------------
		protected enum E作成方法 { ファイルから, WAVファイルイメージから, Unknown }
		protected E作成方法 e作成方法 = E作成方法.Unknown;
		protected ESoundDeviceType eデバイス種別 = ESoundDeviceType.Unknown;
		public string strファイル名 = null;
		protected byte[] byArrWAVファイルイメージ = null;	// WAVファイルイメージ、もしくはchunkのDATA部のみ
		protected GCHandle hGC;
		protected int _hTempoStream = 0;
		protected int _hBassStream = -1;					// ASIO, WASAPI 用
		protected int hBassStream = 0;						// #31076 2013.4.1 yyagi; プロパティとして実装すると動作が低速になったため、
															// tBASSサウンドを作成する_ストリーム生成後の共通処理()のタイミングと、
															// 再生速度を変更したタイミングでのみ、
															// hBassStreamを更新するようにした。
		//{
		//    get
		//    {
		//        if ( _hTempoStream != 0 && !this.bIs1倍速再生 )	// 再生速度がx1.000のときは、TempoStreamを用いないようにして高速化する
		//        {
		//            return _hTempoStream;
		//        }
		//        else
		//        {
		//            return _hBassStream;
		//        }
		//    }
		//    set
		//    {
		//        _hBassStream = value;
		//    }
		//}
		protected SoundBuffer Buffer = null;			// DirectSound 用
		protected DirectSound DirectSound;
		protected int hMixer = -1;	// 設計壊してゴメン Mixerに後で登録するときに使う
		//-----------------
		#endregion

		#region [ private ]
		//-----------------
		private bool bIsDirectSound
		{
			get { return ( this.eデバイス種別 == ESoundDeviceType.DirectSound ); }
		}
		private bool bIsBASS
		{
			get
			{
				return (
					this.eデバイス種別 == ESoundDeviceType.ASIO ||
					this.eデバイス種別 == ESoundDeviceType.ExclusiveWASAPI ||
					this.eデバイス種別 == ESoundDeviceType.SharedWASAPI );
			}
		}
		private int _n位置 = 0;
		private int _n位置db;
		private int _n音量 = 100;
		private int _n音量db;
		private long nBytes = 0;
		private int n一時停止回数 = 0;
		private int nオリジナルの周波数 = 0;
		private double _db周波数倍率 = 1.0;
		private double _db再生速度 = 1.0;
		private bool bIs1倍速再生 = true;

		private void tBASSサウンドを作成する( string strファイル名, int hMixer, BASSFlag flags )
		{
			if ( String.Compare( Path.GetExtension( strファイル名 ), ".xa", true ) == 0 )	// caselessで文字列比較
			{
				tBASSサウンドを作成するXA( strファイル名, hMixer, flags );
				return;
			}

			this.e作成方法 = E作成方法.ファイルから;
			this.strファイル名 = strファイル名;


			// BASSファイルストリームを作成。

			this._hBassStream = Bass.BASS_StreamCreateFile( strファイル名, 0, 0, flags );
			if( this._hBassStream == 0 )
				throw new Exception( string.Format( "サウンドストリームの生成に失敗しました。(BASS_StreamCreateFile)[{0}]", Bass.BASS_ErrorGetCode().ToString() ) );
			
			nBytes = Bass.BASS_ChannelGetLength( this._hBassStream );
			
			tBASSサウンドを作成する_ストリーム生成後の共通処理( hMixer );
		}
		private void tBASSサウンドを作成する( byte[] byArrWAVファイルイメージ, int hMixer, BASSFlag flags )
		{
			this.e作成方法 = E作成方法.WAVファイルイメージから;
			this.byArrWAVファイルイメージ = byArrWAVファイルイメージ;
			this.hGC = GCHandle.Alloc( byArrWAVファイルイメージ, GCHandleType.Pinned );		// byte[] をピン留め


			// BASSファイルストリームを作成。

			this._hBassStream = Bass.BASS_StreamCreateFile( hGC.AddrOfPinnedObject(), 0, byArrWAVファイルイメージ.Length, flags );
			if ( this._hBassStream == 0 )
				throw new Exception( string.Format( "サウンドストリームの生成に失敗しました。(BASS_StreamCreateFile)[{0}]", Bass.BASS_ErrorGetCode().ToString() ) );

			nBytes = Bass.BASS_ChannelGetLength( this._hBassStream );
	
			tBASSサウンドを作成する_ストリーム生成後の共通処理( hMixer );
		}
		private void tBASSサウンドを作成するXA( string strファイル名, int hMixer, BASSFlag flags )
		{
			int nPCMデータの先頭インデックス;
			CWin32.WAVEFORMATEX wfx;
			int totalPCMSize;

			tオンメモリ方式でデコードする( strファイル名, out this.byArrWAVファイルイメージ,
				out nPCMデータの先頭インデックス, out totalPCMSize, out wfx );

			nBytes = totalPCMSize;

			this.e作成方法 = E作成方法.WAVファイルイメージから;		//.ファイルから;	// 再構築時はデコード後のイメージを流用する&Dispose時にhGCを解放する
			this.strファイル名 = strファイル名;
			this.hGC = GCHandle.Alloc( this.byArrWAVファイルイメージ, GCHandleType.Pinned );		// byte[] をピン留め


			_cbStreamXA = new STREAMPROC( CallbackPlayingXA );

			// BASSファイルストリームを作成。

			//this.hBassStream = Bass.BASS_StreamCreate( xa.xaheader.nSamplesPerSec, xa.xaheader.nChannels, BASSFlag.BASS_STREAM_DECODE, _myStreamCreate, IntPtr.Zero );
			this._hBassStream = Bass.BASS_StreamCreate( (int) wfx.nSamplesPerSec, (int) wfx.nChannels, BASSFlag.BASS_STREAM_DECODE, _cbStreamXA, IntPtr.Zero );
			if ( this._hBassStream == 0 )
			{
				hGC.Free();
				throw new Exception( string.Format( "サウンドストリームの生成に失敗しました。(BASS_SampleCreate)[{0}]", Bass.BASS_ErrorGetCode().ToString() ) );
			}

			tBASSサウンドを作成する_ストリーム生成後の共通処理( hMixer );
		}


		private void tBASSサウンドを作成する_ストリーム生成後の共通処理( int hMixer )
		{
			CSoundManager.nStreams++;

			// 個々のストリームの出力をテンポ変更のストリームに入力する。テンポ変更ストリームの出力を、Mixerに出力する。

//			if ( CSoundManager.bIsTimeStretch )	// TimeStretchのON/OFFに関わりなく、テンポ変更のストリームを生成する。後からON/OFF切り替え可能とするため。
			{
				this._hTempoStream = BassFx.BASS_FX_TempoCreate( this._hBassStream, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_FX_FREESOURCE );
				if ( this._hTempoStream == 0 )
				{
					hGC.Free();
					throw new Exception( string.Format( "サウンドストリームの生成に失敗しました。(BASS_FX_TempoCreate)[{0}]", Bass.BASS_ErrorGetCode().ToString() ) );
				}
				else
				{
					Bass.BASS_ChannelSetAttribute( this._hTempoStream, BASSAttribute.BASS_ATTRIB_TEMPO_OPTION_USE_QUICKALGO, 1f );	// 高速化(音の品質は少し落ちる)
				}
			}

			if ( _hTempoStream != 0 && !this.bIs1倍速再生 )	// 再生速度がx1.000のときは、TempoStreamを用いないようにして高速化する
			{
				this.hBassStream = _hTempoStream;
			}
			else
			{
				this.hBassStream = _hBassStream;
			}

			// #32248 再生終了時に発火するcallbackを登録する (演奏終了後に再生終了するチップを非同期的にミキサーから削除するため。)
			_cbEndofStream = new SYNCPROC( CallbackEndofStream );
			Bass.BASS_ChannelSetSync( hBassStream, BASSSync.BASS_SYNC_END | BASSSync.BASS_SYNC_MIXTIME, 0, _cbEndofStream, IntPtr.Zero );

			// インスタンスリストに登録。

			CSound.listインスタンス.Add( this );

			// n総演奏時間の取得; DTXMania用に追加。
			double seconds = Bass.BASS_ChannelBytes2Seconds( this._hBassStream, nBytes );
			this.nTotalPlayTimeMs = (int) ( seconds * 1000 );
			this.pos = 0;
			this.hMixer = hMixer;
			float freq = 0.0f;
			if ( !Bass.BASS_ChannelGetAttribute( this._hBassStream, BASSAttribute.BASS_ATTRIB_FREQ, ref freq ) )
			{
				hGC.Free();
				throw new Exception( string.Format( "サウンドストリームの周波数取得に失敗しました。(BASS_ChannelGetAttribute)[{0}]", Bass.BASS_ErrorGetCode().ToString() ) );
			}
			this.nオリジナルの周波数 = (int) freq;
		}
		//-----------------

		private int pos = 0;
		private int CallbackPlayingXA( int handle, IntPtr buffer, int length, IntPtr user )
		{
			int bytesread = ( pos + length > Convert.ToInt32(nBytes) ) ? Convert.ToInt32(nBytes) - pos : length;

			Marshal.Copy( byArrWAVファイルイメージ, pos, buffer, bytesread );
			pos += bytesread;

			if ( pos >= nBytes )
			{
				// set indicator flag
				bytesread |= (int) BASSStreamProc.BASS_STREAMPROC_END;
			}
			return bytesread;
		}
		/// <summary>
		/// ストリームの終端まで再生したときに呼び出されるコールバック
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="channel"></param>
		/// <param name="data"></param>
		/// <param name="user"></param>
		private void CallbackEndofStream( int handle, int channel, int data, IntPtr user )	// #32248 2013.10.14 yyagi
		{
//			Debug.WriteLine( "Callback!(remove): " + Path.GetFileName( this.strファイル名 ) );
			if ( b演奏終了後も再生が続くチップである )			// 演奏終了後に再生終了するチップ音のミキサー削除は、再生終了のコールバックに引っ掛けて、自前で行う。
			{													// そうでないものは、ミキサー削除予定時刻に削除する。
				tBASSサウンドをミキサーから削除する( channel );
			}
		}

// mixerからの削除

		public bool tBASSサウンドをミキサーから削除する()
		{
			return tBASSサウンドをミキサーから削除する( this.hBassStream );
		}
		public bool tBASSサウンドをミキサーから削除する( int channel )
		{
			bool b = BassMix.BASS_Mixer_ChannelRemove( channel );
			if ( b )
			{
				Interlocked.Decrement( ref CSoundManager.nMixing );
//				Debug.WriteLine( "Removed: " + Path.GetFileName( this.strファイル名 ) + " (" + channel + ")" + " MixedStreams=" + CSoundManager.nMixing );
			}
			return b;
		}


// mixer への追加
		
		public bool tBASSAddSoundToMixer()
		{
			if ( BassMix.BASS_Mixer_ChannelGetMixer( hBassStream ) == 0 )
			{
				BASSFlag bf = BASSFlag.BASS_SPEAKER_FRONT | BASSFlag.BASS_MIXER_NORAMPIN | BASSFlag.BASS_MIXER_PAUSE;
				Interlocked.Increment( ref CSoundManager.nMixing );

				// preloadされることを期待して、敢えてflagからはBASS_MIXER_PAUSEを外してAddChannelした上で、すぐにPAUSEする
				// -> ChannelUpdateでprebufferできることが分かったため、BASS_MIXER_PAUSEを使用することにした

				bool b1 = BassMix.BASS_Mixer_StreamAddChannel( this.hMixer, this.hBassStream, bf );
				//bool b2 = BassMix.BASS_Mixer_ChannelPause( this.hBassStream );
				tSetPlaybackPositionToBeginning();	// StreamAddChannelの後で再生位置を戻さないとダメ。逆だと再生位置が変わらない。
//				Debug.WriteLine( "Add Mixer: " + Path.GetFileName( this.strファイル名 ) + " (" + hBassStream + ")" + " MixedStreams=" + CSoundManager.nMixing );
				Bass.BASS_ChannelUpdate( this.hBassStream, 0 );	// pre-buffer
				return b1;	// &b2;
			}
			return true;
		}

		#region [ tオンメモリ方式でデコードする() ]
		public void tオンメモリ方式でデコードする( string strファイル名, out byte[] buffer,
			out int nPCMデータの先頭インデックス, out int totalPCMSize, out CWin32.WAVEFORMATEX wfx )
		{
			nPCMデータの先頭インデックス = 0;
			//int nPCMサイズbyte = (int) ( xa.xaheader.nSamples * xa.xaheader.nChannels * 2 );	// nBytes = Bass.BASS_ChannelGetLength( this.hBassStream );

			SoundDecoder sounddecoder;

			if ( String.Compare( Path.GetExtension( strファイル名 ), ".xa", true ) == 0 )
			{
				sounddecoder = new Cxa();
			}
			else if ( String.Compare( Path.GetExtension( strファイル名 ), ".ogg", true ) == 0 )
			{
				sounddecoder = new Cogg();
			}
			else if ( String.Compare( Path.GetExtension( strファイル名 ), ".mp3", true ) == 0 )
			{
				sounddecoder = new Cmp3();
			}
			else
			{
				throw new NotImplementedException();
			}

			int nHandle = sounddecoder.Open( strファイル名 );
			if ( nHandle < 0 )
			{
				throw new Exception( string.Format( "Open() に失敗しました。({0})({1})", nHandle, strファイル名 ) );
			}
			wfx = new CWin32.WAVEFORMATEX();
			if ( sounddecoder.GetFormat( nHandle, ref wfx ) < 0 )
			{
				sounddecoder.Close( nHandle );
				throw new Exception( string.Format( "GetFormat() に失敗しました。({0})", strファイル名 ) );
			}
			//totalPCMSize = (int) sounddecoder.nTotalPCMSize;		//  tデコード後のサイズを調べる()で既に取得済みの値を流用する。ms単位の高速化だが、チップ音がたくさんあると塵積で結構効果がある
			totalPCMSize = (int) sounddecoder.GetTotalPCMSize( nHandle );
			if ( totalPCMSize == 0 )
			{
				sounddecoder.Close( nHandle );
				throw new Exception( string.Format( "GetTotalPCMSize() に失敗しました。({0})", strファイル名 ) );
			}
			totalPCMSize += ( ( totalPCMSize % 2 ) != 0 ) ? 1 : 0;
			buffer = new byte[ totalPCMSize ];
			GCHandle handle = GCHandle.Alloc( buffer, GCHandleType.Pinned );
			try
			{
				if ( sounddecoder.Decode( nHandle, handle.AddrOfPinnedObject(), (uint) totalPCMSize, 0 ) < 0 )
				{
					buffer = null;
					throw new Exception( string.Format( "デコードに失敗しました。({0})", strファイル名 ) );
				}
			}
			finally
			{
				handle.Free();
				sounddecoder.Close( nHandle );
				sounddecoder = null;
			}
		}
		#endregion
		#endregion
	}
}