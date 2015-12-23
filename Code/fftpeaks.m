%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

%   CSEE 6893: Big Data Analytics
%   Bridge Health Monitoring
%   Author: Karl S. Bayer (karlsbayer at gmail dot com)
%   Created: 12/2015
  
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
 

function [ peaks, locs, Y, X ] = fftpeaks( acc, i )
%UNTITLED3 Summary of this function goes here
%   Detailed explanation goes here
%%
%sets up 2d matrix with different joints by row and 
% individual time values in columns.
% disp(i)
% load('acc.mat')
srs = 8;
state1 = [  acc(i,2:srs:end); acc(i,3:srs:end); acc(i,4:srs:end); 
            acc(i,5:srs:end); acc(i,6:srs:end); acc(i,7:srs:end); 
            acc(i,8:srs:end); acc(i,9:srs:end); ];
%             acc(i,10:srs:end); acc(i,11:srs:end); acc(i,12:srs:end) ];
len = length(state1);

%[f,yy] = fftDataMag(linspace(0,20,len),state1(1,:));
[f_s1,yy_s1] = my_fftDataMag(linspace(0,20,len),state1(:,:),2);

Y = abs(yy_s1);
X = repmat(f_s1,srs,1);
name = repmat('NPeaks',srs,1);
value = repmat(10,srs,1);
rowparams=table(Y, X, name, value);
%[PKS_s1,LOCS_s1] = findpeaks(mag_s1, [f_s1; f_s1], 'NPeaks',10)

pks = rowfun( @findpeaks, rowparams,'OutputVariableNames',{'PKS_s1' 'LOCS_s1'});

% PKS_s1 = []
% LOCS_s1 = []
% for j=1:2
%     [PKS, LOCS] = findpeaks(Y(j,:),X(j,:), 'NPeaks',10)
%     PKS_s1 = vertcat(PKS_s1,PKS)
%     LOCS_s1 = vertcat(LOCS_s1,PKS)
% end

peaks = [];
locs = [];
for i=1:height(pks)
    peaks = horzcat(peaks, pks{i,1});
    locs = horzcat(locs, pks{i,2});
end
% [peaks.', locs.']




end

