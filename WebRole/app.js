"use strict";

angular.module("WebCrawler", [])
    .controller("DashboardCtrl", function ($scope, $http, $timeout) {
        $scope.title = "Big Bertha";
        var timer;
        function UpdateDashboard() {
            timer = $timeout(function () {
                
            }, 1000);

            timer.then(function () {
                $http.post("Admin.asmx/PullDashboard", {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                }).then(function (response) {
                    $scope.dashboard = JSON.parse(response.data.d);
                    UpdateDashboard();
                });
            });
        }

        UpdateDashboard();

        $scope.startCrawler = function () {
            $http.post("Admin.asmx/StartCrawling", {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            });
        }

        $scope.stopCrawler = function () {
            $http.post("Admin.asmx/StopCrawling", {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            });
        }

        $scope.clearIndex = function () {
            $http.post("Admin.asmx/ClearIndex", {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            });
        }

        $scope.asArray = function (arr) {
            return JSON.parse(arr);
        }
    });
   