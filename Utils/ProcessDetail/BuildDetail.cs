﻿using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services.Access;
using KdxDesigner.Services.Error;
using KdxDesigner.Services.IOAddress;
using KdxDesigner.ViewModels;


namespace KdxDesigner.Utils.ProcessDetail
{
    /// <summary>
    /// 工程詳細をレコード単位でビルドする。
    /// </summary>
    internal class BuildDetail
    {
        protected readonly MainViewModel _mainViewModel;
        protected readonly IErrorAggregator _errorAggregator;
        protected readonly IIOAddressService _ioAddressService;
        protected readonly IAccessRepository _repository;
        protected readonly List<MnemonicDeviceWithProcess> _processes;
        protected readonly List<MnemonicDeviceWithProcessDetail> _details;
        protected readonly List<MnemonicDeviceWithOperation> _operations;
        protected readonly List<MnemonicDeviceWithCylinder> _cylinders;
        protected readonly List<IO> _ioList;

        /// <summary>
        /// 工程詳細のビルドを初期化して、DetailUnitBuilderを使用して各工程の詳細をビルドします。
        /// </summary>
        /// <param name="mainViewModel">MainViewからの初期値</param>
        /// <param name="ioAddressService">IO検索用のサービス</param>
        /// <param name="errorAggregator">エラー出力用のサービス</param>
        /// <param name="repository">ACCESSファイル検索用のリポジトリ</param>
        /// <param name="processes">工程の一覧</param>
        /// <param name="details">工程詳細の一覧</param>
        /// <param name="operations">操作の一覧</param>
        /// <param name="cylinders">CYの一覧</param>
        /// <param name="ioList">IOの一覧</param>
        public BuildDetail(
            MainViewModel mainViewModel,
            IIOAddressService ioAddressService,
            IErrorAggregator errorAggregator,
            IAccessRepository repository,
            List<MnemonicDeviceWithProcess> processes,
            List<MnemonicDeviceWithProcessDetail> details,
            List<MnemonicDeviceWithOperation> operations,
            List<MnemonicDeviceWithCylinder> cylinders,
            List<IO> ioList)
        {
            _mainViewModel = mainViewModel;
            _ioAddressService = ioAddressService;
            _errorAggregator = errorAggregator;
            _repository = repository;
            _processes = processes;
            _details = details;
            _operations = operations;
            _cylinders = cylinders;
            _ioList = ioList;
        }

        /// <summary>
        /// 工程詳細：通常工程のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public List<LadderCsvRow> Normal(MnemonicDeviceWithProcessDetail detail)
        {
            var builder = new DetailUnitBuilder(
                detail, _details, _processes, _operations, _cylinders, _ioList,
                _mainViewModel, _ioAddressService, _errorAggregator, _repository
            );
            return builder.BuildNormal();
        }

        /// <summary>
        /// 工程詳細：工程まとめのビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public List<LadderCsvRow> Summarize(MnemonicDeviceWithProcessDetail detail)
        {
            var builder = new DetailUnitBuilder(
                detail, _details, _processes, _operations, _cylinders, _ioList,
                _mainViewModel, _ioAddressService, _errorAggregator, _repository
            );
            return builder.BuildSummarize();
        }

        /// <summary>
        /// 工程詳細：センサON確認のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public List<LadderCsvRow> SensorON(MnemonicDeviceWithProcessDetail detail)
        {
            var builder = new DetailUnitBuilder(
                detail, _details, _processes, _operations, _cylinders, _ioList,
                _mainViewModel, _ioAddressService, _errorAggregator, _repository
            );
            return builder.BuildSensorON();

        }

        /// <summary>
        /// 工程詳細：センサOFF確認のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public List<LadderCsvRow> SensorOFF(MnemonicDeviceWithProcessDetail detail)
        {
            var builder = new DetailUnitBuilder(
                 detail, _details, _processes, _operations, _cylinders, _ioList,
                 _mainViewModel, _ioAddressService, _errorAggregator, _repository
             );
            return builder.BuildSensorOFF();

        }

        /// <summary>
        /// 工程詳細：工程分岐のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public List<LadderCsvRow> Branch(MnemonicDeviceWithProcessDetail detail)
        {
            var builder = new DetailUnitBuilder(
                 detail, _details, _processes, _operations, _cylinders, _ioList,
                 _mainViewModel, _ioAddressService, _errorAggregator, _repository
             );
            return builder.BuildBranch();
        }

        /// <summary>
        /// 工程詳細：工程合流のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public List<LadderCsvRow> Merge(MnemonicDeviceWithProcessDetail detail)
        {
            var builder = new DetailUnitBuilder(
                 detail, _details, _processes, _operations, _cylinders, _ioList,
                 _mainViewModel, _ioAddressService, _errorAggregator, _repository
             );
            return builder.BuildMerge();

        }

        /// <summary>
        /// 工程詳細：IL待ちのビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public List<LadderCsvRow> ILWait(MnemonicDeviceWithProcessDetail detail)
        {
            var builder = new DetailUnitBuilder(
                 detail, _details, _processes, _operations, _cylinders, _ioList,
                 _mainViewModel, _ioAddressService, _errorAggregator, _repository
             );
            return builder.BuildILWait();
        }

        /// <summary>
        /// 工程詳細：工程OFFのビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public List<LadderCsvRow> ProcessOFF(MnemonicDeviceWithProcessDetail detail)
        {
            var builder = new DetailUnitBuilder(
                 detail, _details, _processes, _operations, _cylinders, _ioList,
                 _mainViewModel, _ioAddressService, _errorAggregator, _repository
             );
            return builder.BuildDetailProcessOFF();

        }

        /// <summary>
        /// 工程詳細：期間工程のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public List<LadderCsvRow> Season(MnemonicDeviceWithProcessDetail detail)
        {
            var builder = new DetailUnitBuilder(
                 detail, _details, _processes, _operations, _cylinders, _ioList,
                 _mainViewModel, _ioAddressService, _errorAggregator, _repository
             );
            return builder.BuildSeason();

        }

        /// <summary>
        /// 工程詳細：タイマ工程のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <param name="detailTimers">タイマの詳細リスト</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public List<LadderCsvRow> TimerProcess(MnemonicDeviceWithProcessDetail detail, List<MnemonicTimerDeviceWithDetail> detailTimers)
        {
            var builder = new DetailUnitBuilder(
                 detail, _details, _processes, _operations, _cylinders, _ioList,
                 _mainViewModel, _ioAddressService, _errorAggregator, _repository
             );
            return builder.BuildTimerProcess(detailTimers);

        }

        /// <summary>
        /// 工程詳細：タイマのビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <param name="detailTimers">タイマの詳細リスト</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public List<LadderCsvRow> Timer(MnemonicDeviceWithProcessDetail detail, List<MnemonicTimerDeviceWithDetail> detailTimers)
        {
            var builder = new DetailUnitBuilder(
                 detail, _details, _processes, _operations, _cylinders, _ioList,
                 _mainViewModel, _ioAddressService, _errorAggregator, _repository
             );
            return builder.BuildTimer(detailTimers);
        }

        /// <summary>
        /// 工程詳細：複数工程のビルド
        /// </summary>
        /// <param name="detail">出力するProcessDetailのレコード</param>
        /// <param name="detailTimers">タイマの詳細リスト</param>
        /// <returns>工程詳細のニモニックリスト</returns>
        public List<LadderCsvRow> Module(MnemonicDeviceWithProcessDetail detail)
        {
            var builder = new DetailUnitBuilder(
                 detail, _details, _processes, _operations, _cylinders, _ioList,
                 _mainViewModel, _ioAddressService, _errorAggregator, _repository
             );
            return builder.BuildModule();
        }
    }
}
