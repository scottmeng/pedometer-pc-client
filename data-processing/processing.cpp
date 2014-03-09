/*
	This is just a fast prototype
 */	

#include <stdio.h>
#include <iostream>
#include <fstream>
#include <string>
#include <sstream>
#include <vector>
#include <algorithm>

using namespace std;

vector<int> timestamps;
vector<int> x_acc;
vector<int> y_acc;
vector<int> z_acc;

vector<int> timestamps_filtered;
vector<int> x_acc_filtered;
vector<int> y_acc_filtered;
vector<int> z_acc_filtered;

vector<int> x_thresholds;
vector<int> y_thresholds;
vector<int> z_thresholds;

vector<int> step_timestamps_x;
vector<int> step_timestamps_y;
vector<int> step_timestamps_z;

#define FILTER_LENGTH 8
#define WINDOW_LENGTH 40
#define DATA_DIR "KAIZHI_7.txt"


void lowPassFilter() {
	int index, i;
	int avg_x, avg_y, avg_z;

	for(index = 0; index < (timestamps.size() - FILTER_LENGTH); index ++) {
		avg_x = 0;
		avg_y = 0;
		avg_z = 0;

		for(i = index; i < (index + FILTER_LENGTH); i ++) {
			avg_x += x_acc[i];
			avg_y += y_acc[i];
			avg_z += z_acc[i];
		}

		avg_x /= FILTER_LENGTH;
		avg_y /= FILTER_LENGTH;
		avg_z /= FILTER_LENGTH;

		timestamps_filtered.push_back(timestamps[index]);
		x_acc_filtered.push_back(avg_x);
		y_acc_filtered.push_back(avg_y);
		z_acc_filtered.push_back(avg_z);
	}
}

void getThresholds() {
	int index, x_threshold, y_threshold, z_threshold;
	vector<int>::iterator begin, end;

	for(index = 0; index < timestamps_filtered.size(); index += WINDOW_LENGTH) {
		begin = x_acc_filtered.begin() + index;
		end = begin + WINDOW_LENGTH - 1;
		x_threshold = (*min_element(begin, end) + *max_element(begin, end)) / 2;

		begin = y_acc_filtered.begin() + index;
		end = begin + WINDOW_LENGTH - 1;
		y_threshold = (*min_element(begin, end) + *max_element(begin, end)) / 2;

		begin = z_acc_filtered.begin() + index;
		end = begin + WINDOW_LENGTH - 1;
		z_threshold = (*min_element(begin, end) + *max_element(begin, end)) / 2;

		x_thresholds.push_back(x_threshold);
		y_thresholds.push_back(y_threshold);
		z_thresholds.push_back(z_threshold);
	}
}

// this is just using x-axis data
void countSteps() {
	int index;

	for(index = 0; index < timestamps_filtered.size(); index ++) {
		if (x_acc_filtered[index] > x_thresholds[index / WINDOW_LENGTH]
			&& x_acc_filtered[index + 1] < x_thresholds[index / WINDOW_LENGTH]) {
			step_timestamps_x.push_back(timestamps_filtered[index]);
		}

		if (y_acc_filtered[index] > y_thresholds[index / WINDOW_LENGTH]
			&& y_acc_filtered[index + 1] < y_thresholds[index / WINDOW_LENGTH]) {
			step_timestamps_y.push_back(timestamps_filtered[index]);
		}

		if (z_acc_filtered[index] > z_thresholds[index / WINDOW_LENGTH]
			&& z_acc_filtered[index + 1] < z_thresholds[index / WINDOW_LENGTH]) {
			step_timestamps_z.push_back(timestamps_filtered[index]);
		}
	}
}

void log(string info) {
	cout << info << endl;
}

int main() {
	int timestamp, x, y, z, index;
	string data;
	ifstream dataFile;
	stringstream ss;

	dataFile.open(DATA_DIR);

	while(getline(dataFile, data)) {
		int size = sscanf(data.c_str(), "%d, %d, %d, %d", &timestamp, &x, &y, &z);

		if(size != 4) {
			exit(0);
		}

		timestamps.push_back(timestamp);
		x_acc.push_back(x);
		y_acc.push_back(y);
		z_acc.push_back(z);
	}

	dataFile.close();

	lowPassFilter();
	
	getThresholds();

	countSteps();

	/*

	for(index = 0; index < step_timestamps_x.size(); index ++) {
		cout << step_timestamps[index] << endl;
	}

	*/

	cout << "Number of steps counted in x-axis is: " << step_timestamps_x.size() << endl;
	cout << "Number of steps counted in y-axis is: " << step_timestamps_y.size() << endl;
	cout << "Number of steps counted in z-axis is: " << step_timestamps_z.size() << endl;

	return 0;
}
