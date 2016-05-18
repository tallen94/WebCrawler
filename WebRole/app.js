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

        $scope.sendCommand = function (command) {
            var data = {
                command: command
            };
            var config = {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            };
            $http.post("Admin.asmx/SendCommand", data, config);
        }

        $scope.sendStart = function () {
            var config = {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            };
            $http.post("Admin.asmx/StartCrawling",config);
        }

        $scope.asArray = function (arr) {
            return JSON.parse(arr);
        }
    });
   