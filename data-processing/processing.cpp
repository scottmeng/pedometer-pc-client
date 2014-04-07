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
#include <math.h>

using namespace std;

vector<int> timestamps;
vector<int> x_acc;
vector<int> y_acc;
vector<int> z_acc;
vector<double> sqrt_acc;

vector<int> timestamps_filtered;
vector<int> x_acc_filtered;
vector<int> y_acc_filtered;
vector<int> z_acc_filtered;
vector<double> sqrt_acc_filtered;

vector<int> timestamps_filtered_g;
vector<int> x_acc_filtered_g;
vector<int> y_acc_filtered_g;
vector<int> z_acc_filtered_g;
vector<double> sqrt_acc_filtered_g;

vector<int> x_thresholds;
vector<int> y_thresholds;
vector<int> z_thresholds;
vector<double> sqrt_thresholds;

vector<int> step_timestamps_x;
vector<int> step_timestamps_y;
vector<int> step_timestamps_z;
vector<int> step_timestamps_sqrt;

// for butterworth filter
double inputValueModifier[4];
double outputValueModifier[4];
double inputValue[4];
double outputValue[4];
int valuePosition;

#define FILTER_LENGTH 8
#define WINDOW_LENGTH 50
#define DATA_DIR "1.txt"
#define GAUSSIAN false
#define LOW_PASS_ORDER 4;

/*
 * low pass fileter using average
 */
void lowPassFilter() {
	int index, i;
	int avg_x, avg_y, avg_z;
	double avg_sqrt;

	for(index = 0; index < (timestamps.size() - FILTER_LENGTH); index ++) {
		avg_x = 0;
		avg_y = 0;
		avg_z = 0;
		avg_sqrt = 0;

		for(i = index; i < (index + FILTER_LENGTH); i ++) {
			avg_x += x_acc[i];
			avg_y += y_acc[i];
			avg_z += z_acc[i];
			avg_sqrt += sqrt_acc[i];
		}

		avg_x /= FILTER_LENGTH;
		avg_y /= FILTER_LENGTH;
		avg_z /= FILTER_LENGTH;
		avg_sqrt /= FILTER_LENGTH;

		timestamps_filtered.push_back(timestamps[index]);
		x_acc_filtered.push_back(avg_x);
		y_acc_filtered.push_back(avg_y);
		z_acc_filtered.push_back(avg_z);
		sqrt_acc_filtered.push_back(avg_sqrt);
	}
}

/*
 * get square root mean of three axis
 */
void preProcessing() {
	int index;
	double processed;

	for (index = 0; index < (timestamps.size()); ++index) {
		processed = sqrt( x_acc[index] * x_acc[index]
						+ y_acc[index] * y_acc[index]
						+ z_acc[index] * z_acc[index]);
		sqrt_acc.push_back(processed);
	}
}

// gaussian function length is 5
void lowPassFilterGaussian() {
	int index;
	double processed_x, processed_y, processed_z, processed_sqrt;

	for (index = 2; index < (timestamps.size() - 3); index ++) {
		processed_x = 0.054 * (x_acc[index - 2] + x_acc[index + 2])
					+ 0.242 * (x_acc[index - 1] + x_acc[index + 1])
					+ 0.399 * x_acc[index];
		processed_y = 0.054 * (y_acc[index - 2] + y_acc[index + 2])
					+ 0.242 * (y_acc[index - 1] + y_acc[index + 1])
					+ 0.399 * y_acc[index];
		processed_z = 0.054 * (z_acc[index - 2] + z_acc[index + 2])
					+ 0.242 * (z_acc[index - 1] + z_acc[index + 1])
					+ 0.399 * z_acc[index];
		processed_sqrt = 0.054 * (sqrt_acc[index - 2] + sqrt_acc[index + 2])
					   + 0.242 * (sqrt_acc[index - 1] + sqrt_acc[index + 1])
					   + 0.399 * sqrt_acc[index];

		timestamps_filtered_g.push_back(timestamps[index]);
		x_acc_filtered_g.push_back(processed_x);
		y_acc_filtered_g.push_back(processed_y);
		z_acc_filtered_g.push_back(processed_z);
		sqrt_acc_filtered_g.push_back(processed_sqrt);
	}
}

void getThresholds() {
	int index, x_threshold, y_threshold, z_threshold;
	double sqrt_threshold;
	vector<int>::iterator begin, end;
	vector<double>::iterator begin_double, end_double;

	if (GAUSSIAN) {
		for(index = 0; index < timestamps_filtered_g.size(); index += WINDOW_LENGTH) {
			begin = x_acc_filtered_g.begin() + index;
			end = begin + WINDOW_LENGTH - 1;
			x_threshold = (*min_element(begin, end) + *max_element(begin, end)) / 2;

			begin = y_acc_filtered_g.begin() + index;
			end = begin + WINDOW_LENGTH - 1;
			y_threshold = (*min_element(begin, end) + *max_element(begin, end)) / 2;

			begin = z_acc_filtered_g.begin() + index;
			end = begin + WINDOW_LENGTH - 1;
			z_threshold = (*min_element(begin, end) + *max_element(begin, end)) / 2;

			begin_double = sqrt_acc_filtered_g.begin() + index;
			end_double = begin_double + WINDOW_LENGTH - 1;
			sqrt_threshold = (*min_element(begin_double, end_double) + *max_element(begin_double, end_double)) / 2;

			x_thresholds.push_back(x_threshold);
			y_thresholds.push_back(y_threshold);
			z_thresholds.push_back(z_threshold);
			sqrt_thresholds.push_back(sqrt_threshold);
		}
	} else {
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

			begin_double = sqrt_acc_filtered.begin() + index;
			end_double = begin_double + WINDOW_LENGTH - 1;
			sqrt_threshold = (*min_element(begin_double, end_double) + *max_element(begin_double, end_double)) / 2;

			x_thresholds.push_back(x_threshold);
			y_thresholds.push_back(y_threshold);
			z_thresholds.push_back(z_threshold);
			sqrt_thresholds.push_back(sqrt_threshold);
		}
	}
}

// this is just using x-axis data
void countSteps() {
	int index;
	int last = 0;

	if (GAUSSIAN) {
		for(index = 0; index < timestamps_filtered_g.size(); index ++) {
			if (x_acc_filtered_g[index] > x_thresholds[index / WINDOW_LENGTH]
				&& x_acc_filtered_g[index + 1] < x_thresholds[index / WINDOW_LENGTH]
				&& (timestamps_filtered_g[index] - last) > 200) {
				step_timestamps_x.push_back(timestamps_filtered_g[index]);
				last = timestamps_filtered_g[index];
			}

			if (y_acc_filtered_g[index] > y_thresholds[index / WINDOW_LENGTH]
				&& y_acc_filtered_g[index + 1] < y_thresholds[index / WINDOW_LENGTH]
				&& (timestamps_filtered_g[index] - last) > 200) {
				step_timestamps_y.push_back(timestamps_filtered_g[index]);
				last = timestamps_filtered_g[index];
			}

			if (z_acc_filtered_g[index] > z_thresholds[index / WINDOW_LENGTH]
				&& z_acc_filtered_g[index + 1] < z_thresholds[index / WINDOW_LENGTH]
				&& (timestamps_filtered_g[index] - last) > 200) {
				step_timestamps_z.push_back(timestamps_filtered_g[index]);
				last = timestamps_filtered_g[index];
			}

			if (sqrt_acc_filtered_g[index] > sqrt_thresholds[index / WINDOW_LENGTH]
				&& sqrt_acc_filtered_g[index + 1] < sqrt_thresholds[index / WINDOW_LENGTH]
				&& (timestamps_filtered_g[index] - last) > 200) {
				step_timestamps_sqrt.push_back(timestamps_filtered_g[index]);
				last = timestamps_filtered_g[index];
			}
		}
	} else {
		for(index = 0; index < timestamps_filtered.size(); index ++) {
			if (x_acc_filtered[index] > x_thresholds[index / WINDOW_LENGTH]
				&& x_acc_filtered[index + 1] < x_thresholds[index / WINDOW_LENGTH]
				&& (timestamps_filtered[index] - last) > 200) {
				step_timestamps_x.push_back(timestamps_filtered[index]);
				last = timestamps_filtered[index];
			}

			if (y_acc_filtered[index] > y_thresholds[index / WINDOW_LENGTH]
				&& y_acc_filtered[index + 1] < y_thresholds[index / WINDOW_LENGTH]
				&& (timestamps_filtered[index] - last) > 200) {
				step_timestamps_y.push_back(timestamps_filtered[index]);
				last = timestamps_filtered[index];
			}

			if (z_acc_filtered[index] > z_thresholds[index / WINDOW_LENGTH]
				&& z_acc_filtered[index + 1] < z_thresholds[index / WINDOW_LENGTH]
				&& (timestamps_filtered[index] - last) > 200) {
				step_timestamps_z.push_back(timestamps_filtered[index]);
				last = timestamps_filtered[index];
			}

			if (sqrt_acc_filtered[index] > sqrt_thresholds[index / WINDOW_LENGTH]
				&& sqrt_acc_filtered[index + 1] < sqrt_thresholds[index / WINDOW_LENGTH]
				&& (timestamps_filtered[index] - last) > 200) {
				step_timestamps_sqrt.push_back(timestamps_filtered[index]);
				last = timestamps_filtered[index];
			}
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

    inputValueModifier[0] = 0.0180989;
    inputValueModifier[1] = 0.0542968;
    inputValueModifier[2] = 0.0542968;
    inputValueModifier[3] = 0.0180989;

    outputValueModifier[0] = 1.0;
    outputValueModifier[1] = -1.76004;
    outputValueModifier[2] = 1.18289;
    outputValueModifier[3] = -0.27806;

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

	preProcessing();

	if (GAUSSIAN) {
		lowPassFilterGaussian();
	} else {
		lowPassFilter();
	}
	
	getThresholds();

	countSteps();

	cout << "Number of steps counted in x-axis is: " << step_timestamps_x.size() * 2 << endl;
	cout << "Number of steps counted in y-axis is: " << step_timestamps_y.size() * 2<< endl;
	cout << "Number of steps counted in z-axis is: " << step_timestamps_z.size() * 2<< endl;
	cout << "Average: " << (step_timestamps_x.size() * 2 + step_timestamps_y.size() * 2 + step_timestamps_z.size() * 2) / 3 << endl;
	cout << "Number of steps counted in processed-axis is: " << step_timestamps_sqrt.size() << endl;
	
	return 0;
}
